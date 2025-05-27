namespace Webinex.Calendar;

public class Event<TData> : IEvent<TData> where TData : class, ICloneable
{
    public string Id { get; protected set; } = null!;
    public string TimeZone { get; protected set; } = null!;
    public Period<DateTimeOffset> Period { get; protected set; } = null!;
    public EventGroupId Group { get; protected set; } = null!;
    public TData Data { get; protected set; } = null!;
    public Recurrence? Recurrence { get; protected set; } = null;

    protected Event()
    {
    }

    public Event(
        string id,
        string timeZone,
        Period<DateTimeOffset> period,
        EventGroupId groupId,
        TData data,
        Recurrence? recurrence = null)
    {
        Guard.NotNull(id).ASCII(id);
        Guard.NotNull(timeZone);
        Guard.NotNull(period);
        Guard.NotNull(groupId);
        Guard.NotNull(data);

        Id = id;
        TimeZone = timeZone;
        Period = period.Clone();
        Group = groupId.Clone();
        Data = (TData)data.Clone();
        Recurrence = recurrence?.Clone();
    }

    public static Event<TData> New(
        Period<DateTimeOffset> period,
        string timeZone,
        TData data,
        Recurrence? recurrence = null,
        string? id = null,
        EventGroupId? group = null)
    {
        id ??= EventId.New(recurrence == null ? EventType.OneTime : EventType.Recurrent);
        group ??= EventGroupId.New();

        return new Event<TData>(id, timeZone, period, group, data, recurrence);
    }

    public void SetData(TData data)
    {
        if (Data != data)
            Data = (TData)data.Clone();
    }

    public void SetPeriod(Period<DateTimeOffset> period)
    {
        if (Period == period)
            return;

        Period = period.Clone();
    }

    public void SetTimeZone(string timeZone)
    {
        TimeZone = timeZone ?? throw new ArgumentNullException(nameof(timeZone));
    }

    public void SetEndDate(DateOnly value)
    {
        if (Recurrence == null)
            throw new InvalidOperationException($"Unexpected call to {nameof(SetEndDate)} for non recurrent event");

        Recurrence = Recurrence.WithEnd(value);
    }

    public void SetRecurrence(Recurrence? recurrence)
    {
        if ((Recurrence == null) != (recurrence == null))
            throw new InvalidOperationException($"It's forbidden to convert recurrent event to non recurrent and vice versa. {Id}");
        
        if (Recurrence != recurrence)
            Recurrence = recurrence?.Clone();
    }
    
    public TimeSpan Duration() => Period.End - Period.Start;
}