using System.Collections.Immutable;
using Webinex.Asky;
using Webinex.Calendar.DataAccess;

namespace Webinex.Calendar.Caches;

internal class NoCache<TData> : ICache<TData>
    where TData : class, ICloneable
{
    public bool TryGetAll(
        DateTimeOffset from,
        DateTimeOffset to,
        FilterRule? dataFilterRule,
        out ImmutableArray<EventRow<TData>>? result)
    {
        result = null;
        return false;
    }

    public void Push(IEnumerable<CacheEvent<TData>> values)
    {
    }

    public Task Flush()
    {
        return Task.CompletedTask;
    }
}