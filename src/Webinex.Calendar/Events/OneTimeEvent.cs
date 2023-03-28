using Webinex.Calendar.Common;

namespace Webinex.Calendar.Events;

public class OneTimeEvent<TData> : IEvent
    where TData : class
{
    protected OneTimeEvent()
    {
    }

    public Guid Id { get; protected set; }
    public Period Period { get; protected set; } = null!;
    public TData Data { get; protected set; } = null!;
    public bool Cancelled { get; protected set; }
    EventType IEvent.Type => EventType.OneTimeEvent;

    public static OneTimeEvent<TData> New(Guid id, Period period, TData data)
    {
        return new OneTimeEvent<TData>
        {
            Id = id,
            Period = period,
            Data = data,
        };
    }

    public static OneTimeEvent<TData> New(Period period, TData data)
    {
        return New(Guid.NewGuid(), period, data);
    }

    public Event<TData> ToEvent()
    {
        return new Event<TData>(Id, null, Period, Data, Cancelled, null);
    }
}