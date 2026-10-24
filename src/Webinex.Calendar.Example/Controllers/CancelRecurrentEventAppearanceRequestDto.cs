namespace Webinex.Calendar.Example.Controllers;

public class CancelRecurrentEventAppearanceRequestDto
{
    public string RecurrentEventId { get; init; } = null!;
    public DateTimeOffset EventStart { get; init; }
}
