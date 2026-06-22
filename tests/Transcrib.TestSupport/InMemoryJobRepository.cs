using Transcrib.Shared.Models;
using Transcrib.Shared.Services;

namespace Transcrib.Tests.Common;

/// <summary>In-memory <see cref="IJobRepository"/> for deterministic unit tests.</summary>
public class InMemoryJobRepository : IJobRepository
{
    private readonly Dictionary<string, TranscriptionJob> _store = new();

    public Task<TranscriptionJob> CreateAsync(TranscriptionJob job, CancellationToken cancellationToken = default)
    {
        job.CreatedAt = DateTimeOffset.UtcNow;
        job.UpdatedAt = job.CreatedAt;
        _store[job.Id] = job;
        return Task.FromResult(job);
    }

    public Task<TranscriptionJob?> GetAsync(string id, CancellationToken cancellationToken = default)
        => Task.FromResult(_store.TryGetValue(id, out var job) ? job : null);

    public Task<IReadOnlyList<TranscriptionJob>> ListAsync(CancellationToken cancellationToken = default)
        => Task.FromResult((IReadOnlyList<TranscriptionJob>)_store.Values
            .OrderByDescending(j => j.CreatedAt).ToList());

    public Task<TranscriptionJob> UpsertAsync(TranscriptionJob job, CancellationToken cancellationToken = default)
    {
        job.UpdatedAt = DateTimeOffset.UtcNow;
        _store[job.Id] = job;
        return Task.FromResult(job);
    }
}
