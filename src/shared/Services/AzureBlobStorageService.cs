using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace Transcrib.Shared.Services;

/// <summary>
/// Azure Blob Storage implementation of <see cref="IBlobStorageService"/>.
/// Uses a <see cref="BlobServiceClient"/> wired with Managed Identity.
/// </summary>
public class AzureBlobStorageService : IBlobStorageService
{
    private readonly BlobServiceClient _client;

    public AzureBlobStorageService(BlobServiceClient client)
    {
        _client = client;
    }

    public async Task<BlobUploadResult> UploadAudioAsync(string blobName, Stream content, string contentType, CancellationToken cancellationToken = default)
    {
        var container = _client.GetBlobContainerClient(BlobContainers.Audio);
        await container.CreateIfNotExistsAsync(cancellationToken: cancellationToken);

        var blob = container.GetBlobClient(blobName);

        await blob.UploadAsync(
            content,
            new BlobUploadOptions { HttpHeaders = new BlobHttpHeaders { ContentType = contentType } },
            cancellationToken);

        return new BlobUploadResult(blobName, blob.Uri.ToString());
    }

    public async Task<BlobUploadResult> UploadTranscriptAsync(string blobName, string content, CancellationToken cancellationToken = default)
    {
        var container = _client.GetBlobContainerClient(BlobContainers.Transcripts);
        await container.CreateIfNotExistsAsync(cancellationToken: cancellationToken);

        var blob = container.GetBlobClient(blobName);
        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(content));
        await blob.UploadAsync(
            stream,
            new BlobUploadOptions { HttpHeaders = new BlobHttpHeaders { ContentType = "text/plain; charset=utf-8" } },
            cancellationToken);

        return new BlobUploadResult(blobName, blob.Uri.ToString());
    }

    public async Task<(Stream Content, string ContentType)?> DownloadTranscriptAsync(string blobName, CancellationToken cancellationToken = default)
    {
        var container = _client.GetBlobContainerClient(BlobContainers.Transcripts);
        var blob = container.GetBlobClient(blobName);

        if (!await blob.ExistsAsync(cancellationToken))
        {
            return null;
        }

        var response = await blob.DownloadStreamingAsync(cancellationToken: cancellationToken);
        var contentType = response.Value.Details.ContentType ?? "text/plain";
        return (response.Value.Content, contentType);
    }
}
