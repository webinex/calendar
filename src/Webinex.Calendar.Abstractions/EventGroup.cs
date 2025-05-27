namespace Webinex.Calendar;

public class EventGroup
{
    public Guid Id { get; }
    public DateOnly Start { get; }
    public DateOnly? End { get; }

    public EventGroup(Guid id, DateOnly start, DateOnly? end)
    {
        Id = id;
        Start = start;
        End = end;
    }
}