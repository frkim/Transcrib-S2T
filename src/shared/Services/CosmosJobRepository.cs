using Microsoft.Azure.Cosmos;
using Transcrib.Shared.Models;

namespace Transcrib.Shared.Services;

/// <summary>
/// Cosmos DB backed implementation of <see cref="IJobRepository"/> targeting the
/// shared <c>jobs</c> container (partition key: <c>/id</c>).
/// </summary>
public class CosmosJobRepository : IJobRepository
{
    private readonly Container _container;

    public CosmosJobRepository(Container container)
    {
        _container = container;
    }

    public async Task<TranscriptionJob> CreateAsync(TranscriptionJob job, CancellationToken cancellationToken = default)
    {
        job.CreatedAt = DateTimeOffset.UtcNow;
        job.UpdatedAt = job.CreatedAt;
        var response = await _container.CreateItemAsync(job, new PartitionKey(job.Id), cancellationToken: cancellationToken);
        return response.Resource;
    }

    public async Task<TranscriptionJob?> GetAsync(string id, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _container.ReadItemAsync<TranscriptionJob>(id, new PartitionKey(id), cancellationToken: cancellationToken);
            return response.Resource;
        }
        catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    public async Task<IReadOnlyList<TranscriptionJob>> ListAsync(CancellationToken cancellationToken = default)
    {
        var query = new QueryDefinition("SELECT * FROM c ORDER BY c.createdAt DESC");
        var results = new List<TranscriptionJob>();
        using var iterator = _container.GetItemQueryIterator<TranscriptionJob>(query);
        while (iterator.HasMoreResults)
        {
            var page = await iterator.ReadNextAsync(cancellationToken);
            results.AddRange(page);
        }

        return results;
    }

    public async Task<TranscriptionJob> UpsertAsync(TranscriptionJob job, CancellationToken cancellationToken = default)
    {
        job.UpdatedAt = DateTimeOffset.UtcNow;
        var response = await _container.UpsertItemAsync(job, new PartitionKey(job.Id), cancellationToken: cancellationToken);
        return response.Resource;
    }

    public async Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        try
        {
            await _container.DeleteItemAsync<TranscriptionJob>(id, new PartitionKey(id), cancellationToken: cancellationToken);
            return true;
        }
        catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return false;
        }
    }
}
