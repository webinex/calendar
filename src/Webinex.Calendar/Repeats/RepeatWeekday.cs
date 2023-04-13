using Webinex.Calendar.Common;

namespace Webinex.Calendar.Repeats;

public class RepeatWeekday : Equatable, IRepeatBase
{
    protected RepeatWeekday()
    {
    }

    // public int? OvernightDurationMinutes { get; protected set; }
    // public int SameDayLastTime { get; protected set; }
    public Weekday[] Weekdays { get; protected set; } = null!;

    public int TimeOfTheDayUtcMinutes { get; protected set; }
    public int DurationMinutes { get; protected set; }

    public static RepeatWeekday New(RepeatWeekday value)
    {
        return new RepeatWeekday
        {
            Weekdays = value.Weekdays.Select(x => new Weekday(x.Value)).ToArray(),
            TimeOfTheDayUtcMinutes = value.TimeOfTheDayUtcMinutes,
            DurationMinutes = value.DurationMinutes,
        };
    }

    internal static RepeatWeekday New(
        int timeOfTheDayUtcMinutes,
        int durationMinutes,
        Weekday[] weekdays)
    {
        if (!weekdays.Any())
            throw new InvalidOperationException($"{nameof(weekdays)} might contain at least one weekday");

        if (durationMinutes > TimeSpan.FromDays(1).TotalMinutes)
            throw new InvalidOperationException("Duration cannot be more than 1 day");

        if (timeOfTheDayUtcMinutes < 0)
            throw new ArgumentException("Might be >= 0", nameof(timeOfTheDayUtcMinutes));

        if (durationMinutes < 0)
            throw new ArgumentException("Might be >= 0", nameof(durationMinutes));

        return new RepeatWeekday
        {
            Weekdays = weekdays,
            DurationMinutes = durationMinutes,
            TimeOfTheDayUtcMinutes = timeOfTheDayUtcMinutes,
        };
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        return new object?[] { TimeOfTheDayUtcMinutes, DurationMinutes, Weekdays }.Concat(Weekdays);
    }

    public static bool operator ==(RepeatWeekday? left, RepeatWeekday? right)
    {
        return EqualOperator(left, right);
    }

    public static bool operator !=(RepeatWeekday? left, RepeatWeekday? right)
    {
        return NotEqualOperator(left, right);
    }
}