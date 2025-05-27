namespace Webinex.Calendar;

public class EventGroupId : Equatable
{
    public Guid Id { get; protected init; }
    
    /// <summary>
    ///     Offset, in minutes, from the original scheduled time of the event.
    /// </summary>
    public int Offset { get; protected init; }

    public EventGroupId(Guid id, TimeSpan offset)
    {
        Id = id;
        Offset = (int)offset.TotalMinutes;
    }

    public EventGroupId(Guid id, int offset)
    {
        Id = id;
        Offset = offset;
    }

    public EventGroupId(EventGroupId value)
    {
        value = value ?? throw new ArgumentNullException(nameof(value));
        Id = value.Id;
        Offset = value.Offset;
    }

    protected EventGroupId()
    {
    }

    public EventGroupId Clone() => new(this);

    public static EventGroupId New()
    {
        var id = Guid.NewGuid();
        return new EventGroupId(id, TimeSpan.Zero);
    }

    public TimeSpan OffsetTimeSpan() => TimeSpan.FromMinutes(Offset);
    
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Id;
        yield return Offset;
    }
}