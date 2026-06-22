using Transcrib.Shared.Models;
using Transcrib.Shared.Services;

namespace Transcrib.Api.Services;

/// <summary>A file submitted for transcription, decoupled from ASP.NET's IFormFile for testability.</summary>
public record UploadFile(string FileName, string? ContentType, Stream Content);

public record CreateJobsResult(IReadOnlyList<TranscriptionJob> Created, IReadOnlyList<string> Rejected);

/// <summary>
/// Business logic for the jobs API. Decoupled from HTTP primitives so it can be
/// unit tested with in-memory fakes for Blob Storage and Cosmos DB.
/// </summary>
public class JobService
{
    private readonly IJobRepository _jobs;
    private readonly IBlobStorageService _blobs;
    private readonly ILogger<JobService> _logger;

    public JobService(IJobRepository jobs, IBlobStorageService blobs, ILogger<JobService> logger)
    {
        _jobs = jobs;
        _blobs = blobs;
        _logger = logger;
    }

    public async Task<CreateJobsResult> CreateJobsAsync(IEnumerable<UploadFile> files, CancellationToken cancellationToken = default)
    {
        var created = new List<TranscriptionJob>();
        var rejected = new List<string>();

        foreach (var file in files)
        {
            if (!AudioFileValidator.IsValidMp3(file.FileName, file.ContentType))
            {
                _logger.LogWarning("Rejected non-MP3 upload {FileName}", file.FileName);
                rejected.Add(file.FileName);
                continue;
            }

            try
            {
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
                    file.Content,
                    AudioFileValidator.AllowedContentType,
                    cancellationToken);

                job.AudioBlobUrl = upload.BlobUrl;

                var saved = await _jobs.CreateAsync(job, cancellationToken);
                created.Add(saved);
                _logger.LogInformation("Created transcription job {JobId} for {FileName}", saved.Id, file.FileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create job for {FileName}", file.FileName);
                rejected.Add(file.FileName);
            }
        }

        return new CreateJobsResult(created, rejected);
    }

    public Task<IReadOnlyList<TranscriptionJob>> ListJobsAsync(CancellationToken cancellationToken = default)
        => _jobs.ListAsync(cancellationToken);

    public Task<TranscriptionJob?> GetJobAsync(string id, CancellationToken cancellationToken = default)
        => _jobs.GetAsync(id, cancellationToken);

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
}
