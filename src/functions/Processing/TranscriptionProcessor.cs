using Microsoft.Extensions.Logging;
using Transcrib.Functions.Services;
using Transcrib.Shared.Models;
using Transcrib.Shared.Services;

namespace Transcrib.Functions.Processing;

/// <summary>
/// Core transcription pipeline logic, decoupled from the Functions runtime so it
/// can be unit tested. Mirrors the Logic Apps workflow functionally:
/// Processing → Speech-to-Text (diarization) → write transcript → Completed/Failed.
/// </summary>
public class TranscriptionProcessor
{
    private const int MaxAttempts = 2; // simple retry

    private readonly IJobRepository _jobs;
    private readonly ITranscriptionService _transcription;
    private readonly IBlobStorageService _blobs;
    private readonly ILogger<TranscriptionProcessor> _logger;

    public TranscriptionProcessor(
        IJobRepository jobs,
        ITranscriptionService transcription,
        IBlobStorageService blobs,
        ILogger<TranscriptionProcessor> logger)
    {
        _jobs = jobs;
        _transcription = transcription;
        _blobs = blobs;
        _logger = logger;
    }

    /// <summary>
    /// Processes a newly uploaded audio blob. <paramref name="blobName"/> is the
    /// audio blob name (<c>{jobId}.mp3</c>); the job id is derived from it.
    /// </summary>
    public async Task<TranscriptionJob?> ProcessAsync(string blobName, Stream audio, CancellationToken cancellationToken = default)
    {
        var jobId = Path.GetFileNameWithoutExtension(blobName);
        var job = await _jobs.GetAsync(jobId, cancellationToken);
        if (job is null)
        {
            _logger.LogWarning("No job found for blob {BlobName}", blobName);
            return null;
        }

        job.Status = JobStatus.Processing;
        job.Error = null;
        await _jobs.UpsertAsync(job, cancellationToken);

        Exception? lastError = null;
        for (var attempt = 1; attempt <= MaxAttempts; attempt++)
        {
            try
            {
                if (audio.CanSeek)
                {
                    audio.Position = 0;
                }

                var result = await _transcription.TranscribeAsync(audio, job.FileName, cancellationToken);

                var transcriptBlobName = $"{job.Id}.txt";
                var upload = await _blobs.UploadTranscriptAsync(transcriptBlobName, result.Text, cancellationToken);

                job.TranscriptBlobUrl = upload.BlobUrl;
                job.Status = JobStatus.Completed;
                job.Error = null;
                await _jobs.UpsertAsync(job, cancellationToken);

                _logger.LogInformation("Job {JobId} completed (attempt {Attempt})", job.Id, attempt);
                return job;
            }
            catch (Exception ex)
            {
                lastError = ex;
                _logger.LogWarning(ex, "Transcription attempt {Attempt} failed for job {JobId}", attempt, job.Id);
            }
        }

        job.Status = JobStatus.Failed;
        job.Error = lastError?.Message ?? "Transcription failed.";
        await _jobs.UpsertAsync(job, cancellationToken);
        _logger.LogError(lastError, "Job {JobId} failed after {MaxAttempts} attempts", job.Id, MaxAttempts);
        return job;
    }
}
