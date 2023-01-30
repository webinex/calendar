namespace Webinex.Calendar.Events;

public class OneTimeEvent<TData>
    where TData : class
{
    protected OneTimeEvent()
    {
    }

    public Guid Id { get; protected set; }
    public DateTimeOffset Start { get; protected set; }
    public DateTimeOffset End { get; protected set; }
    public TData Data { get; protected set; } = null!;

    public static OneTimeEvent<TData> New(Guid id, DateTimeOffset start, DateTimeOffset end, TData data)
    {
        if (start > end)
            throw new InvalidOperationException($"{nameof(start)} cannot be greater than {nameof(end)}");
        
        return new OneTimeEvent<TData>
        {
            Id = id,
            Start = start,
            End = end,
            Data = data,
        };
    }

    public static OneTimeEvent<TData> New(DateTimeOffset start, DateTimeOffset end, TData data)
    {
        return New(Guid.NewGuid(), start, end, data);
    }

    public Event<TData> ToEvent()
    {
        // TODO: s.skalaban, allow one time event cancellation
        return new Event<TData>(Id, null, Start, End, Data, false);
    }
}