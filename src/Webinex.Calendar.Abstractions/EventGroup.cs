namespace Webinex.Calendar;

public class EventGroup : Equatable
{
    public Guid Id { get; protected init; }
    public TimeSpan Offset { get; protected init; }

    public EventGroup(Guid id, TimeSpan offset)
    {
        Id = id;
        Offset = offset;
    }

    public EventGroup(EventGroup value)
    {
        value = value ?? throw new ArgumentNullException(nameof(value));
        Id = value.Id;
        Offset = value.Offset;
    }

    protected EventGroup()
    {
    }

    public EventGroup Clone() => new(this);

    public static EventGroup New()
    {
        var id = Guid.NewGuid();
        return new EventGroup(id, TimeSpan.Zero);
    }
    
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Id;
        yield return Offset;
    }
}