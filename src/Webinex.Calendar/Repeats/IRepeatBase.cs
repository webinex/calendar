namespace Webinex.Calendar.Repeats;

public interface IRepeatBase
{
    int TimeOfTheDayUtcMinutes { get; }
    int DurationMinutes { get; }
}