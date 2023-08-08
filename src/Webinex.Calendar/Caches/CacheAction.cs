using System.Collections.Concurrent;
using Webinex.Calendar.DataAccess;

namespace Webinex.Calendar.Caches;

internal enum CacheEventType
{
    Add,
    Delete,
    Update,
}

internal abstract record CacheEvent<TData>(CacheEventType Type, EventRow<TData> Value) where TData : class, ICloneable
{
    public abstract bool TryApply(ConcurrentDictionary<EventRowId, EventRow<TData>> data);

    public record Add(EventRow<TData> Row) : CacheEvent<TData>(CacheEventType.Add, Row)
    {
        public override bool TryApply(ConcurrentDictionary<EventRowId, EventRow<TData>> data)
        {
            return data.TryAdd(Row.GetEventRowId(), Row);
        }
    }

    public record Delete(EventRow<TData> Row) : CacheEvent<TData>(CacheEventType.Delete, Row)
    {
        public override bool TryApply(ConcurrentDictionary<EventRowId, EventRow<TData>> data)
        {
            return data.TryRemove(Row.GetEventRowId(), out _);
        }
    }

    public record Update(EventRow<TData> Row) : CacheEvent<TData>(CacheEventType.Update, Row)
    {
        public override bool TryApply(ConcurrentDictionary<EventRowId, EventRow<TData>> data)
        {
            return data.TryUpdate(Row.GetEventRowId(), Row, data[Row.GetEventRowId()]);
        }
    }
}