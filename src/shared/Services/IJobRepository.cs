using Transcrib.Shared.Models;

namespace Transcrib.Shared.Services;

/// <summary>
/// Abstraction over the Cosmos DB <c>jobs</c> container. Allows the API and the
/// Functions pipeline to share the same persistence contract and to be tested
/// without a live Cosmos DB instance.
/// </summary>
public interface IJobRepository
{
    Task<TranscriptionJob> CreateAsync(TranscriptionJob job, CancellationToken cancellationToken = default);

    Task<TranscriptionJob?> GetAsync(string id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TranscriptionJob>> ListAsync(CancellationToken cancellationToken = default);

    Task<TranscriptionJob> UpsertAsync(TranscriptionJob job, CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default);
}
