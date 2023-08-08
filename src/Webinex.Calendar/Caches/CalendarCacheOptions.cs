using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Webinex.Calendar.Common;

namespace Webinex.Calendar.Caches;

internal class CalendarCacheOptions
{
    internal static readonly TimeSpan TIMER_TICK = TimeSpan.FromSeconds(15);
    
    protected CalendarCacheOptions(bool enabled, TimeSpan? previous, TimeSpan? next, TimeSpan? tick)
    {
        Enabled = enabled;
        Previous = previous;
        Next = next;
        Tick = tick;

        if (enabled && tick!.Value < TIMER_TICK)
            throw new ArgumentException($"Cannot be less than {TIMER_TICK:c}", nameof(tick));
    }

    [MemberNotNullWhen(true, nameof(Previous), nameof(Next))]
    public bool Enabled { get; private set; }

    public TimeSpan? Previous { get; private set; }
    public TimeSpan? Next { get; private set; }
    public TimeSpan? Tick { get; private set; }

    internal DateTimeOffset Min() => DateTimeOffset.UtcNow.StartOfMinute().Subtract(Previous!.Value);
    internal DateTimeOffset Max() => DateTimeOffset.UtcNow.StartOfMinute().Add(Next!.Value);

    internal static object NewEnabled(Type dataType, TimeSpan lt, TimeSpan gte, TimeSpan tick)
    {
        return typeof(CalendarCacheOptions<>).MakeGenericType(dataType).GetConstructors(BindingFlags.NonPublic | BindingFlags.Instance).Single()
            .Invoke(
                new object?[]
                {
                    true,
                    lt,
                    gte,
                    tick
                });
    }

    internal static object NewNotEnabled(Type dataType)
    {
        return typeof(CalendarCacheOptions<>).MakeGenericType(dataType).GetConstructors(BindingFlags.NonPublic | BindingFlags.Instance).Single()
            .Invoke(
                new object?[]
                {
                    true,
                    null,
                    null,
                    null
                });
    }
}

internal class CalendarCacheOptions<TData> : CalendarCacheOptions where TData : class, ICloneable
{
    protected CalendarCacheOptions(bool enabled, TimeSpan? previous, TimeSpan? next, TimeSpan? tick) : base(enabled, previous, next,
        tick)
    {
    }
}