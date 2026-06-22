using Microsoft.Extensions.Logging.Abstractions;
using Transcrib.Api.Services;
using Transcrib.Shared.Models;
using Transcrib.Tests.Common;
using Xunit;

namespace Transcrib.Api.Tests;

public class JobServiceTests
{
    private static JobService CreateService(out InMemoryJobRepository jobs, out InMemoryBlobStorageService blobs)
    {
        jobs = new InMemoryJobRepository();
        blobs = new InMemoryBlobStorageService();
        return new JobService(jobs, blobs, NullLogger<JobService>.Instance);
    }

    private static UploadFile Mp3(string name = "sample.mp3")
        => new(name, "audio/mpeg", new MemoryStream(new byte[] { 1, 2, 3 }));

    [Fact]
    public async Task CreateJobsAsync_ValidMp3_CreatesProcessingJob()
    {
        var service = CreateService(out _, out var blobs);

        var result = await service.CreateJobsAsync(new[] { Mp3() });

        var job = Assert.Single(result.Created);
        Assert.Empty(result.Rejected);
        Assert.Equal(JobStatus.Processing, job.Status);
        Assert.Equal("sample.mp3", job.FileName);
        Assert.Contains($"{job.Id}.mp3", job.AudioBlobUrl);
        Assert.True(blobs.Audio.ContainsKey($"{job.Id}.mp3"));
    }

    [Fact]
    public async Task CreateJobsAsync_MultipleFiles_CreatesMultipleJobs()
    {
        var service = CreateService(out _, out _);

        var result = await service.CreateJobsAsync(new[] { Mp3("a.mp3"), Mp3("b.mp3") });

        Assert.Equal(2, result.Created.Count);
    }

    [Fact]
    public async Task CreateJobsAsync_NonMp3_IsRejected()
    {
        var service = CreateService(out _, out _);

        var result = await service.CreateJobsAsync(new[]
        {
            new UploadFile("notes.txt", "text/plain", new MemoryStream(new byte[] { 1 }))
        });

        Assert.Empty(result.Created);
        Assert.Equal("notes.txt", Assert.Single(result.Rejected));
    }

    [Fact]
    public async Task GetTranscriptAsync_WhenCompleted_ReturnsTranscript()
    {
        var service = CreateService(out var jobs, out var blobs);
        var created = (await service.CreateJobsAsync(new[] { Mp3() })).Created.Single();

        // Simulate the transcription pipeline completing the job.
        blobs.Transcripts[$"{created.Id}.txt"] = "Speaker Guest-1: hello world";
        created.TranscriptBlobUrl = $"https://example.blob.core.windows.net/transcripts/{created.Id}.txt";
        created.Status = JobStatus.Completed;
        await jobs.UpsertAsync(created);

        var transcript = await service.GetTranscriptAsync(created.Id);

        Assert.NotNull(transcript);
        using var reader = new StreamReader(transcript!.Value.Content);
        Assert.Contains("hello world", reader.ReadToEnd());
        Assert.Equal("sample.txt", transcript.Value.FileName);
    }

    [Fact]
    public async Task GetTranscriptAsync_WhenNotReady_ReturnsNull()
    {
        var service = CreateService(out _, out _);
        var created = (await service.CreateJobsAsync(new[] { Mp3() })).Created.Single();

        var transcript = await service.GetTranscriptAsync(created.Id);

        Assert.Null(transcript);
    }
}
