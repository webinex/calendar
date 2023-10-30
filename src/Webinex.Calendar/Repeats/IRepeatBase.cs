namespace Webinex.Calendar.Repeats;

public interface IRepeatBase
{
    int TimeOfTheDayInMinutes { get; }
    int DurationMinutes { get; }
}