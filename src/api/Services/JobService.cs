using Transcrib.Shared.Models;
using Transcrib.Shared.Services;

namespace Transcrib.Api.Services;

/// <summary>A file submitted for transcription, decoupled from ASP.NET's IFormFile for testability.</summary>
public record UploadFile(string FileName, string? ContentType, Stream Content);

/// <summary>A file that could not be turned into a job, together with the reason why.</summary>
public record RejectedUpload(string FileName, string Reason);

public record CreateJobsResult(
    IReadOnlyList<TranscriptionJob> Created,
    IReadOnlyList<RejectedUpload> Rejected,
    bool LimitReached);

/// <summary>
/// Business logic for the jobs API. Decoupled from HTTP primitives so it can be
/// unit tested with in-memory fakes for Blob Storage and Cosmos DB.
/// </summary>
public class JobService
{
    private readonly IJobRepository _jobs;
    private readonly IBlobStorageService _blobs;
    private readonly IAudioDurationProbe _durationProbe;
    private readonly TranscriptionLimits _limits;
    private readonly ILogger<JobService> _logger;

    public JobService(
        IJobRepository jobs,
        IBlobStorageService blobs,
        IAudioDurationProbe durationProbe,
        TranscriptionLimits limits,
        ILogger<JobService> logger)
    {
        _jobs = jobs;
        _blobs = blobs;
        _durationProbe = durationProbe;
        _limits = limits;
        _logger = logger;
    }

    public async Task<CreateJobsResult> CreateJobsAsync(IEnumerable<UploadFile> files, CancellationToken cancellationToken = default)
    {
        var created = new List<TranscriptionJob>();
        var rejected = new List<RejectedUpload>();
        var limitReached = false;

        // Rate limits are enforced against the jobs created within rolling
        // windows. Counters are seeded from persisted jobs and incremented as
        // this batch is processed so a single upload cannot exceed the quota.
        var now = DateTimeOffset.UtcNow;
        var existing = await _jobs.ListAsync(cancellationToken);
        var dayCount = existing.Count(j => j.CreatedAt > now - TimeSpan.FromDays(1));
        var weekCount = existing.Count(j => j.CreatedAt > now - TimeSpan.FromDays(7));

        foreach (var file in files)
        {
            if (!AudioFileValidator.IsValidMp3(file.FileName, file.ContentType))
            {
                _logger.LogWarning("Rejected non-MP3 upload {FileName}", file.FileName);
                rejected.Add(new RejectedUpload(file.FileName, "Only .mp3 files are accepted."));
                continue;
            }

            try
            {
                // Buffer the upload so the duration can be inspected before the
                // audio is streamed to Blob Storage.
                byte[] audio;
                using (var buffer = new MemoryStream())
                {
                    await file.Content.CopyToAsync(buffer, cancellationToken);
                    audio = buffer.ToArray();
                }

                var duration = _durationProbe.TryGetDuration(audio);
                if (duration is { } length && length >= _limits.MaxDuration)
                {
                    _logger.LogWarning(
                        "Rejected {FileName}: duration {Duration} exceeds limit {Limit}",
                        file.FileName, length, _limits.MaxDuration);
                    rejected.Add(new RejectedUpload(
                        file.FileName,
                        $"Audio is too long ({length:mm\\:ss}); maximum is {_limits.MaxDurationMinutes} minutes."));
                    continue;
                }

                if (dayCount >= _limits.MaxPerDay)
                {
                    limitReached = true;
                    rejected.Add(new RejectedUpload(
                        file.FileName,
                        $"Daily limit reached ({_limits.MaxPerDay} transcriptions per day)."));
                    continue;
                }

                if (weekCount >= _limits.MaxPerWeek)
                {
                    limitReached = true;
                    rejected.Add(new RejectedUpload(
                        file.FileName,
                        $"Weekly limit reached ({_limits.MaxPerWeek} transcriptions per week)."));
                    continue;
                }

                var job = new TranscriptionJob
                {
                    FileName = file.FileName,
                    Status = JobStatus.Processing
                };

                // Audio blob is keyed by job id so the transcription pipeline can
                // correlate the uploaded file back to its Cosmos DB job.
                var blobName = $"{job.Id}.mp3";
                var upload = await _blobs.UploadAudioAsync(
                    blobName,
                    new MemoryStream(audio),
                    AudioFileValidator.AllowedContentType,
                    cancellationToken);

                job.AudioBlobUrl = upload.BlobUrl;

                var saved = await _jobs.CreateAsync(job, cancellationToken);
                created.Add(saved);
                dayCount++;
                weekCount++;
                _logger.LogInformation("Created transcription job {JobId} for {FileName}", saved.Id, file.FileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create job for {FileName}", file.FileName);
                rejected.Add(new RejectedUpload(file.FileName, "Upload failed."));
            }
        }

        return new CreateJobsResult(created, rejected, limitReached);
    }

    public Task<IReadOnlyList<TranscriptionJob>> ListJobsAsync(CancellationToken cancellationToken = default)
        => _jobs.ListAsync(cancellationToken);

    public Task<TranscriptionJob?> GetJobAsync(string id, CancellationToken cancellationToken = default)
        => _jobs.GetAsync(id, cancellationToken);

    public async Task<TranscriptionJob?> UpdateStatusAsync(
        string id,
        string status,
        string? transcriptBlobUrl = null,
        string? error = null,
        CancellationToken cancellationToken = default)
    {
        var job = await _jobs.GetAsync(id, cancellationToken);
        if (job is null)
        {
            return null;
        }

        job.Status = status;
        if (transcriptBlobUrl is not null)
        {
            job.TranscriptBlobUrl = transcriptBlobUrl;
        }
        job.Error = error;

        var updated = await _jobs.UpsertAsync(job, cancellationToken);
        _logger.LogInformation("Job {JobId} status updated to {Status}", id, status);
        return updated;
    }

    public async Task<(Stream Content, string ContentType, string FileName)?> GetTranscriptAsync(string id, CancellationToken cancellationToken = default)
    {
        var job = await _jobs.GetAsync(id, cancellationToken);
        if (job is null || string.IsNullOrEmpty(job.TranscriptBlobUrl))
        {
            return null;
        }

        var blobName = new Uri(job.TranscriptBlobUrl).Segments.Last();
        var download = await _blobs.DownloadTranscriptAsync(blobName, cancellationToken);
        if (download is null)
        {
            return null;
        }

        var transcriptFileName = Path.GetFileNameWithoutExtension(job.FileName) + ".txt";
        return (download.Value.Content, download.Value.ContentType, transcriptFileName);
    }

    /// <summary>
    /// Deletes a job and its associated audio/transcript blobs. Returns
    /// <c>false</c> when no job exists for the supplied id.
    /// </summary>
    public async Task<bool> DeleteJobAsync(string id, CancellationToken cancellationToken = default)
    {
        var job = await _jobs.GetAsync(id, cancellationToken);
        if (job is null)
        {
            return false;
        }

        if (!string.IsNullOrEmpty(job.AudioBlobUrl))
        {
            var audioBlobName = new Uri(job.AudioBlobUrl).Segments.Last();
            await _blobs.DeleteAudioAsync(audioBlobName, cancellationToken);
        }

        if (!string.IsNullOrEmpty(job.TranscriptBlobUrl))
        {
            var transcriptBlobName = new Uri(job.TranscriptBlobUrl).Segments.Last();
            await _blobs.DeleteTranscriptAsync(transcriptBlobName, cancellationToken);
        }

        var deleted = await _jobs.DeleteAsync(id, cancellationToken);
        _logger.LogInformation("Deleted transcription job {JobId}", id);
        return deleted;
    }
}
