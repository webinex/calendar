using Webinex.Calendar.Common;

namespace Webinex.Calendar.Repeats;

public class RepeatDayOfMonth : Equatable, IRepeatBase
{
    protected RepeatDayOfMonth()
    {
    }

    public DayOfMonth DayOfMonth { get; protected set; } = null!;
    public int TimeOfTheDayUtcMinutes { get; protected set; }
    public int DurationMinutes { get; protected set; }

    public static RepeatDayOfMonth New(RepeatDayOfMonth value)
    {
        return new RepeatDayOfMonth
        {
            DurationMinutes = value.DurationMinutes,
            DayOfMonth = new DayOfMonth(value.DayOfMonth.Value),
            TimeOfTheDayUtcMinutes = value.TimeOfTheDayUtcMinutes,
        };
    }
    
    internal static RepeatDayOfMonth New(
        int timeOfTheDayUtcMinutes,
        int durationMinutes,
        DayOfMonth dayOfMonth)
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
            TimeOfTheDayUtcMinutes = timeOfTheDayUtcMinutes,
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
        yield return TimeOfTheDayUtcMinutes;
        yield return DurationMinutes;
        yield return DayOfMonth;
    }
}