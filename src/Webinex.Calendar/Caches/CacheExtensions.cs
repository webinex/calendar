using Webinex.Calendar.DataAccess;

namespace Webinex.Calendar.Caches;

internal static class CacheExtensions
{
    public static void PushAdd<TData>(this ICache<TData> cache, EventRow<TData> value)
        where TData : class, ICloneable =>
        cache.Push(new[] { new CacheEvent<TData>.Add(value) });

    public static void PushAdd<TData>(this ICache<TData> cache, IEnumerable<EventRow<TData>> values)
        where TData : class, ICloneable
        => cache.Push(values.Select(val => new CacheEvent<TData>.Add(val)).ToArray());

    public static void PushDelete<TData>(this ICache<TData> cache, EventRow<TData> value)
        where TData : class, ICloneable =>
        cache.Push(new[] { new CacheEvent<TData>.Delete(value) });

    public static void PushDelete<TData>(this ICache<TData> cache, IEnumerable<EventRow<TData>> values)
        where TData : class, ICloneable
        => cache.Push(values.Select(val => new CacheEvent<TData>.Delete(val)).ToArray());

    public static void PushUpdate<TData>(this ICache<TData> cache, EventRow<TData> value)
        where TData : class, ICloneable =>
        cache.Push(new[] { new CacheEvent<TData>.Update(value) });

    public static void PushUpdate<TData>(this ICache<TData> cache, IEnumerable<EventRow<TData>> values)
        where TData : class, ICloneable
        => cache.Push(values.Select(val => new CacheEvent<TData>.Update(val)).ToArray());
}