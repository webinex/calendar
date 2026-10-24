namespace Webinex.Calendar.Example.Controllers;

public class CancelRecurrentEventRequestDto
{
    public string RecurrentEventId { get; init; } = null!;
    public DateTimeOffset Since { get; init; }
}
