namespace Transcrib.Functions.Services;

/// <summary>
/// Produces a short-lived, read-only URL for a blob of the <c>audio</c> container so that
/// Azure AI Speech Batch Transcription (which pulls audio by URL) can read it without any
/// account key. Abstracted for unit testing.
/// </summary>
public interface IAudioSasUrlProvider
{
    Task<Uri> GetReadUrlAsync(string audioBlobName, CancellationToken cancellationToken = default);
}
