using Microsoft.Extensions.Logging.Abstractions;
using Transcrib.Functions.Processing;
using Transcrib.Functions.Services;
using Transcrib.Shared.Models;
using Transcrib.Tests.Common;
using Xunit;

namespace Transcrib.Functions.Tests;

public class TranscriptionProcessorTests
{
    private sealed class StubTranscriptionService : ITranscriptionService
    {
        private readonly Func<TranscriptionResult> _factory;
        public int Calls { get; private set; }

        public StubTranscriptionService(Func<TranscriptionResult> factory) => _factory = factory;

        public Task<TranscriptionResult> TranscribeAsync(Stream audio, string fileName, CancellationToken cancellationToken = default)
        {
            Calls++;
            return Task.FromResult(_factory());
        }
    }

    private static async Task<TranscriptionJob> SeedJobAsync(InMemoryJobRepository jobs)
    {
        var job = new TranscriptionJob
        {
            FileName = "meeting.mp3",
            AudioBlobUrl = "https://example/audio/x.mp3",
            Status = JobStatus.Processing
        };
        return await jobs.CreateAsync(job);
    }

    [Fact]
    public async Task ProcessAsync_Success_WritesTranscriptAndCompletesJob()
    {
        var jobs = new InMemoryJobRepository();
        var blobs = new InMemoryBlobStorageService();
        var job = await SeedJobAsync(jobs);
        var transcriber = new StubTranscriptionService(() => new TranscriptionResult("Speaker Guest-1: hi"));
        var processor = new TranscriptionProcessor(jobs, transcriber, blobs, NullLogger<TranscriptionProcessor>.Instance);

        var result = await processor.ProcessAsync($"{job.Id}.mp3", new MemoryStream(new byte[] { 1 }));

        Assert.NotNull(result);
        Assert.Equal(JobStatus.Completed, result!.Status);
        Assert.NotNull(result.TranscriptBlobUrl);
        Assert.Equal("Speaker Guest-1: hi", blobs.Transcripts[$"{job.Id}.txt"]);
    }

    [Fact]
    public async Task ProcessAsync_TranscriptionThrows_RetriesThenMarksFailed()
    {
        var jobs = new InMemoryJobRepository();
        var blobs = new InMemoryBlobStorageService();
        var job = await SeedJobAsync(jobs);
        var transcriber = new StubTranscriptionService(() => throw new InvalidOperationException("speech down"));
        var processor = new TranscriptionProcessor(jobs, transcriber, blobs, NullLogger<TranscriptionProcessor>.Instance);

        var result = await processor.ProcessAsync($"{job.Id}.mp3", new MemoryStream(new byte[] { 1 }));

        Assert.NotNull(result);
        Assert.Equal(JobStatus.Failed, result!.Status);
        Assert.Equal("speech down", result.Error);
        Assert.True(transcriber.Calls >= 2); // simple retry attempted
    }

    [Fact]
    public async Task ProcessAsync_UnknownBlob_ReturnsNull()
    {
        var jobs = new InMemoryJobRepository();
        var blobs = new InMemoryBlobStorageService();
        var transcriber = new StubTranscriptionService(() => new TranscriptionResult("x"));
        var processor = new TranscriptionProcessor(jobs, transcriber, blobs, NullLogger<TranscriptionProcessor>.Instance);

        var result = await processor.ProcessAsync("does-not-exist.mp3", new MemoryStream(new byte[] { 1 }));

        Assert.Null(result);
    }
}
