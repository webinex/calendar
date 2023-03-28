namespace Webinex.Calendar.Example.Controllers;

public class CancelRecurrentEventRequestDto
{
    public Guid RecurrentEventId { get; init; }
    public DateTimeOffset Since { get; init; }
}