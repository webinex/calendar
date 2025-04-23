using Webinex.Asky;

namespace Webinex.Calendar.Caches;

internal class NoCache<TData> : ICache<TData>
    where TData : class, ICloneable
{
    public bool TryGetAll(
        Period<DateTimeOffset> period,
        FilterRule? dataFilterRule,
        out IReadOnlyCollection<IEventEntityBase>? result)
    {
        result = null;
        return false;
    }

    public void Push(IEnumerable<CacheEvent<TData>> values)
    {
    }

    public void Flush()
    {
    }
}