using Transcrib.Shared.Services;

namespace Transcrib.Tests.Common;

/// <summary>In-memory <see cref="IBlobStorageService"/> for deterministic unit tests.</summary>
public class InMemoryBlobStorageService : IBlobStorageService
{
    public Dictionary<string, byte[]> Audio { get; } = new();
    public Dictionary<string, string> Transcripts { get; } = new();

    private const string BaseUrl = "https://example.blob.core.windows.net";

    public async Task<BlobUploadResult> UploadAudioAsync(string blobName, Stream content, string contentType, CancellationToken cancellationToken = default)
    {
        using var ms = new MemoryStream();
        await content.CopyToAsync(ms, cancellationToken);
        Audio[blobName] = ms.ToArray();
        return new BlobUploadResult(blobName, $"{BaseUrl}/{BlobContainers.Audio}/{blobName}");
    }

    public Task<BlobUploadResult> UploadTranscriptAsync(string blobName, string content, CancellationToken cancellationToken = default)
    {
        Transcripts[blobName] = content;
        return Task.FromResult(new BlobUploadResult(blobName, $"{BaseUrl}/{BlobContainers.Transcripts}/{blobName}"));
    }

    public Task<(Stream Content, string ContentType)?> DownloadTranscriptAsync(string blobName, CancellationToken cancellationToken = default)
    {
        if (!Transcripts.TryGetValue(blobName, out var text))
        {
            return Task.FromResult<(Stream, string)?>(null);
        }

        var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(text));
        return Task.FromResult<(Stream, string)?>((stream, "text/plain"));
    }

    public Task DeleteAudioAsync(string blobName, CancellationToken cancellationToken = default)
    {
        Audio.Remove(blobName);
        return Task.CompletedTask;
    }

    public Task DeleteTranscriptAsync(string blobName, CancellationToken cancellationToken = default)
    {
        Transcripts.Remove(blobName);
        return Task.CompletedTask;
    }
}
