using Microsoft.Extensions.Logging.Abstractions;
using Transcrib.Api.Services;
using Transcrib.Shared.Models;
using Transcrib.Tests.Common;
using Xunit;

namespace Transcrib.Api.Tests;

public class JobServiceTests
{
    private static JobService CreateService(
        out InMemoryJobRepository jobs,
        out InMemoryBlobStorageService blobs,
        TranscriptionLimits? limits = null,
        TimeSpan? duration = null)
    {
        jobs = new InMemoryJobRepository();
        blobs = new InMemoryBlobStorageService();
        return new JobService(
            jobs,
            blobs,
            new FakeAudioDurationProbe(duration),
            limits ?? new TranscriptionLimits(),
            NullLogger<JobService>.Instance);
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
        Assert.Equal("notes.txt", Assert.Single(result.Rejected).FileName);
    }
    [Fact]
    public async Task CreateJobsAsync_TooLong_IsRejected()
    {
        var service = CreateService(out _, out _, duration: TimeSpan.FromMinutes(6));

        var result = await service.CreateJobsAsync(new[] { Mp3() });

        Assert.Empty(result.Created);
        var rejected = Assert.Single(result.Rejected);
        Assert.Equal("sample.mp3", rejected.FileName);
        Assert.Contains("too long", rejected.Reason, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CreateJobsAsync_UnderDurationLimit_IsAccepted()
    {
        var service = CreateService(out _, out _, duration: TimeSpan.FromMinutes(4));

        var result = await service.CreateJobsAsync(new[] { Mp3() });

        Assert.Single(result.Created);
        Assert.Empty(result.Rejected);
    }

    [Fact]
    public async Task CreateJobsAsync_DailyLimit_RejectsExcess()
    {
        var limits = new TranscriptionLimits { MaxPerDay = 3, MaxPerWeek = 10 };
        var service = CreateService(out _, out _, limits);

        var result = await service.CreateJobsAsync(new[]
        {
            Mp3("a.mp3"), Mp3("b.mp3"), Mp3("c.mp3"), Mp3("d.mp3")
        });

        Assert.Equal(3, result.Created.Count);
        var rejected = Assert.Single(result.Rejected);
        Assert.Equal("d.mp3", rejected.FileName);
        Assert.True(result.LimitReached);
    }

    [Fact]
    public async Task CreateJobsAsync_DailyLimit_CountsExistingJobsInWindow()
    {
        var limits = new TranscriptionLimits { MaxPerDay = 3, MaxPerWeek = 10 };
        var service = CreateService(out var jobs, out _, limits);
        for (var i = 0; i < 3; i++)
        {
            await jobs.CreateAsync(new TranscriptionJob { FileName = $"old-{i}.mp3" });
        }

        var result = await service.CreateJobsAsync(new[] { Mp3("new.mp3") });

        Assert.Empty(result.Created);
        Assert.True(result.LimitReached);
        Assert.Equal("new.mp3", Assert.Single(result.Rejected).FileName);
    }

    [Fact]
    public async Task CreateJobsAsync_WeeklyLimit_RejectsExcess()
    {
        var limits = new TranscriptionLimits { MaxPerDay = 100, MaxPerWeek = 2 };
        var service = CreateService(out _, out _, limits);

        var result = await service.CreateJobsAsync(new[]
        {
            Mp3("a.mp3"), Mp3("b.mp3"), Mp3("c.mp3")
        });

        Assert.Equal(2, result.Created.Count);
        Assert.Equal("c.mp3", Assert.Single(result.Rejected).FileName);
        Assert.True(result.LimitReached);
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

    [Fact]
    public async Task UpdateStatusAsync_UpdatesStatusAndTranscriptUrl()
    {
        var service = CreateService(out _, out _);
        var created = (await service.CreateJobsAsync(new[] { Mp3() })).Created.Single();

        var updated = await service.UpdateStatusAsync(
            created.Id,
            JobStatus.Completed,
            "https://example/transcripts/x.txt");

        Assert.NotNull(updated);
        Assert.Equal(JobStatus.Completed, updated!.Status);
        Assert.Equal("https://example/transcripts/x.txt", updated.TranscriptBlobUrl);
    }

    [Fact]
    public async Task UpdateStatusAsync_UnknownJob_ReturnsNull()
    {
        var service = CreateService(out _, out _);

        var updated = await service.UpdateStatusAsync("missing", JobStatus.Purged);

        Assert.Null(updated);
    }

    [Fact]
    public async Task DeleteJobAsync_RemovesJobAndBlobs()
    {
        var service = CreateService(out var jobs, out var blobs);
        var created = (await service.CreateJobsAsync(new[] { Mp3() })).Created.Single();

        // Simulate a completed transcript so both blobs exist.
        blobs.Transcripts[$"{created.Id}.txt"] = "transcript text";
        created.TranscriptBlobUrl = $"https://example.blob.core.windows.net/transcripts/{created.Id}.txt";
        created.Status = JobStatus.Completed;
        await jobs.UpsertAsync(created);

        var deleted = await service.DeleteJobAsync(created.Id);

        Assert.True(deleted);
        Assert.Null(await jobs.GetAsync(created.Id));
        Assert.False(blobs.Audio.ContainsKey($"{created.Id}.mp3"));
        Assert.False(blobs.Transcripts.ContainsKey($"{created.Id}.txt"));
    }

    [Fact]
    public async Task DeleteJobAsync_UnknownJob_ReturnsFalse()
    {
        var service = CreateService(out _, out _);

        var deleted = await service.DeleteJobAsync("missing");

        Assert.False(deleted);
    }
}
