using Webinex.Calendar.Common;

namespace Webinex.Calendar.Repeats;

public class RepeatDayOfMonth : Equatable, IRepeatBase
{
    protected RepeatDayOfMonth()
    {
    }

    public DayOfMonth DayOfMonth { get; protected set; } = null!;
    public int TimeOfTheDayInMinutes { get; protected set; }
    public int DurationMinutes { get; protected set; }
    public TimeZoneInfo TimeZone { get; protected set; } = null!;

    public static RepeatDayOfMonth New(RepeatDayOfMonth value)
    {
        return new RepeatDayOfMonth
        {
            DurationMinutes = value.DurationMinutes,
            DayOfMonth = new DayOfMonth(value.DayOfMonth.Value),
            TimeOfTheDayInMinutes = value.TimeOfTheDayInMinutes,
            TimeZone = value.TimeZone,
        };
    }
    
    internal static RepeatDayOfMonth New(
        int timeOfTheDayUtcMinutes,
        int durationMinutes,
        DayOfMonth dayOfMonth,
        TimeZoneInfo timeZone)
    {
        if (durationMinutes > TimeSpan.FromDays(1).TotalMinutes)
            throw new InvalidOperationException("Duration cannot be more than 1 day");

        if (timeOfTheDayUtcMinutes < 0)
            throw new ArgumentException("Might be >= 0", nameof(timeOfTheDayUtcMinutes));

        if (durationMinutes < 0)
            throw new ArgumentException("Might be >= 0", nameof(durationMinutes));

        return new RepeatDayOfMonth
        {
            DayOfMonth = dayOfMonth,
            DurationMinutes = durationMinutes,
            TimeOfTheDayInMinutes = timeOfTheDayUtcMinutes,
            TimeZone = timeZone,
        };
    }

    public static bool operator ==(RepeatDayOfMonth? left, RepeatDayOfMonth? right)
    {
        return EqualOperator(left, right);
    }

    public static bool operator !=(RepeatDayOfMonth? left, RepeatDayOfMonth? right)
    {
        return NotEqualOperator(left, right);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return TimeOfTheDayInMinutes;
        yield return DurationMinutes;
        yield return DayOfMonth;
        yield return TimeZone;
    }
}