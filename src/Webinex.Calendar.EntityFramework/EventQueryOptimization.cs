namespace Webinex.Calendar.EntityFramework;

public class EventQueryOptimization
{
    /// <summary>
    ///     Optimizes index as explicitly define a range for an effective.start.
    ///     <c>event.Effective.Start &lt; period.End &amp;&amp; event.Effective.Start &gt;= period.Start.Subtract(EffectiveStartWindow.Value)</c>
    /// </summary>
    public TimeSpan? EffectiveStartWindow { get; init; }

    public EventQueryOptimization()
    {
    }

    public static EventQueryOptimization Disable => new() { EffectiveStartWindow = null };

    /// <summary>
    ///     EffectiveStartWindow:
    ///     Optimizes index as explicitly define a range for an effective.start
    ///     we assume a limitation of max event duration is 24h and max move is 72h
    /// </summary>
    public static EventQueryOptimization Default => new() { EffectiveStartWindow = TimeSpan.FromDays(4) };

    protected EventQueryOptimization(EventQueryOptimization value)
    {
        EffectiveStartWindow = value.EffectiveStartWindow;
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
