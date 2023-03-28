namespace Webinex.Calendar.Example.Controllers;

public class EditEventTimeRequestDto
{
    public Guid RecurrentEventId { get; init; }
    public DateTimeOffset EventStart { get; init; }
    public DateTimeOffset MoveToStart { get; init; }
    public DateTimeOffset MoveToEnd { get; init; }
}