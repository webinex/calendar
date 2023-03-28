namespace Webinex.Calendar.Example.Controllers;

public class CancelRecurrentEventAppearanceRequestDto
{
    public Guid RecurrentEventId { get; init; }
    public DateTimeOffset EventStart { get; init; }
}