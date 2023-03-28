using Webinex.Calendar.Common;

namespace Webinex.Calendar.Events;

public class RecurrentEventState<TData> : IEvent
    where TData : class
{
    public Guid RecurrentEventId { get; protected set; }
    public Period Period { get; protected set; } = null!;
    public Period? MoveTo { get; protected set; }
    public TData Data { get; protected set; } = null!;
    public bool Cancelled { get; protected set; }
    EventType IEvent.Type => EventType.RecurrentEventState;

    public Event<TData> ToEvent()
    {
        var period = MoveTo ?? Period;
        return new Event<TData>(null, RecurrentEventId, period, Data, Cancelled, Period);
    }

    public static RecurrentEventState<TData> New(
        Guid recurrentEventId,
        Period period,
        Period? moveTo,
        bool cancelled,
        TData data)
    {
        return new RecurrentEventState<TData>
        {
            RecurrentEventId = recurrentEventId,
            Period = period,
            MoveTo = moveTo,
            Cancelled = cancelled,
            Data = data,
        };
    }
}