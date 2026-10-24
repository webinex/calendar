namespace Webinex.Calendar.EntityFramework;

public class EventQueryOptimization
{
    /// <summary>
    ///     Optimizes index as explicitly define a range for an effective.start
    ///     by default: we assume a limitation of max event duration is 24h and max move is 72h
    /// </summary>
    public TimeSpan? MaxStartOffset { get; init; } = TimeSpan.FromDays(4);

    /// <summary>
    ///     Skip recurrent events search
    /// </summary>
    public bool NoRecurrentEvents { get; init; } = false;

    public EventQueryOptimization()
    {
    }

    public static EventQueryOptimization Disable => new() { MaxStartOffset = null, NoRecurrentEvents = false };

    protected EventQueryOptimization(EventQueryOptimization value)
    {
        MaxStartOffset = value.MaxStartOffset;
        NoRecurrentEvents = value.NoRecurrentEvents;
    }
}

public class EventQueryOptimization<TData> : EventQueryOptimization
{
    public EventQueryOptimization()
    {
    }

    public EventQueryOptimization(EventQueryOptimization value) : base(value)
    {
    }

    public static EventQueryOptimization<TData> New(EventQueryOptimization value)
    {
        return new EventQueryOptimization<TData>(value);
    }
}