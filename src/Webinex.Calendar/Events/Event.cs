namespace Webinex.Calendar.Events;

public class Event<TData>
    where TData : class
{
    public Event(Guid? id, Guid? recurringEventId, DateTimeOffset start, DateTimeOffset end, TData data, bool cancelled)
    {
        Id = id;
        RecurringEventId = recurringEventId;
        Start = start;
        End = end;
        Data = data;
        Cancelled = cancelled;
    }

    public Guid? Id { get; }
    public Guid? RecurringEventId { get; }
    public DateTimeOffset Start { get; }
    public DateTimeOffset End { get; }
    public TData Data { get; }
    public bool Cancelled { get; }
}