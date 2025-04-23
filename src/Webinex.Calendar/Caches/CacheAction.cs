using System.Collections.Concurrent;

namespace Webinex.Calendar.Caches;

internal enum CacheEventType
{
    Add,
    Delete,
    Update,
}

internal abstract record CacheEvent<TData>(CacheEventType Type, IEventEntityBase Value) where TData : class, ICloneable
{
    public abstract bool TryApply(ConcurrentDictionary<string, IEventEntityBase> data);

    public record Add(IEventEntityBase Event) : CacheEvent<TData>(CacheEventType.Add, Event)
    {
        public override bool TryApply(ConcurrentDictionary<string, IEventEntityBase> data)
        {
            return data.TryAdd(Event.Id, Event);
        }
    }

    public record Delete(IEventEntityBase Event) : CacheEvent<TData>(CacheEventType.Delete, Event)
    {
        public override bool TryApply(ConcurrentDictionary<string, IEventEntityBase> data)
        {
            return data.TryRemove(Event.Id, out _);
        }
    }

    public record Update(IEventEntityBase Event) : CacheEvent<TData>(CacheEventType.Update, Event)
    {
        public override bool TryApply(ConcurrentDictionary<string, IEventEntityBase> data)
        {
            return data.TryUpdate(Event.Id, Event, data[Event.Id]);
        }
    }
}