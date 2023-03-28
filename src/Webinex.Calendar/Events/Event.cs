using Webinex.Calendar.Common;

namespace Webinex.Calendar.Events;

public class Event<TData>
    where TData : class
{
    public Event(Guid? id, Guid? recurringEventId, Period period, TData data, bool cancelled, Period? movedFrom)
    {
        Id = id;
        RecurringEventId = recurringEventId;
        Start = period.Start;
        End = period.End;
        Data = data;
        Cancelled = cancelled;
        MovedFrom = movedFrom;
    }

    public Guid? Id { get; }
    public Guid? RecurringEventId { get; }
    public DateTimeOffset Start { get; }
    public DateTimeOffset End { get; }
    public Period? MovedFrom { get; }
    public TData Data { get; }
    public bool Cancelled { get; }
}