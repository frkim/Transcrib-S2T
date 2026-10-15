using Azure.Storage.Blobs;
using Azure.Storage.Sas;
using Transcrib.Shared.Services;

namespace Transcrib.Functions.Services;

/// <summary>
/// Builds a read-only user delegation SAS URL for an audio blob. The SAS is signed with a
/// Microsoft Entra ID user delegation key (Managed Identity), so no storage account key is
/// ever used. The identity needs the <c>Storage Blob Delegator</c> role on the account.
/// </summary>
public class BlobAudioSasUrlProvider : IAudioSasUrlProvider
{
    private static readonly TimeSpan ClockSkew = TimeSpan.FromMinutes(5);

    private readonly BlobServiceClient _client;
    private readonly TimeSpan _lifetime;

    public BlobAudioSasUrlProvider(BlobServiceClient client, TimeSpan lifetime)
    {
        _client = client;
        _lifetime = lifetime;
    }

    public async Task<Uri> GetReadUrlAsync(string audioBlobName, CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        var startsOn = now - ClockSkew;
        var expiresOn = now + _lifetime;

        var key = await _client.GetUserDelegationKeyAsync(startsOn, expiresOn, cancellationToken);

        var blob = _client.GetBlobContainerClient(BlobContainers.Audio).GetBlobClient(audioBlobName);
        var sas = new BlobSasBuilder(BlobSasPermissions.Read, expiresOn)
        {
            BlobContainerName = blob.BlobContainerName,
            BlobName = blob.Name,
            Resource = "b",
            StartsOn = startsOn,
        };

        var uri = new BlobUriBuilder(blob.Uri)
        {
            Sas = sas.ToSasQueryParameters(key.Value, _client.AccountName),
        };
        return uri.ToUri();
    }
}
