namespace Webinex.Calendar.Example.Controllers;

public class EditEventTimeRequestDto
{
    public string RecurrentEventId { get; init; } = null!;
    public DateTimeOffset EventStart { get; init; }
    public DateTimeOffset MoveToStart { get; init; }
    public DateTimeOffset MoveToEnd { get; init; }
}
