namespace Transcrib.Shared.Services;

public record BlobUploadResult(string BlobName, string BlobUrl);

/// <summary>
/// Abstraction over Blob Storage used to upload source MP3 files and to read
/// generated transcripts. Implemented by <see cref="AzureBlobStorageService"/>.
/// </summary>
public interface IBlobStorageService
{
    Task<BlobUploadResult> UploadAudioAsync(string blobName, Stream content, string contentType, CancellationToken cancellationToken = default);

    Task<BlobUploadResult> UploadTranscriptAsync(string blobName, string content, CancellationToken cancellationToken = default);

    Task<(Stream Content, string ContentType)?> DownloadTranscriptAsync(string blobName, CancellationToken cancellationToken = default);
}
