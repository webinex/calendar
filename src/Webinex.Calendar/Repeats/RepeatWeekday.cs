using Webinex.Calendar.Common;

namespace Webinex.Calendar.Repeats;

public class RepeatWeekday : Equatable, IRepeatBase
{
    protected RepeatWeekday()
    {
    }

    public Weekday[] Weekdays { get; protected set; } = null!;

    public int TimeOfTheDayInMinutes { get; protected set; }
    public int DurationMinutes { get; protected set; }
    public int? Interval { get; protected set; }
    public TimeZoneInfo TimeZone { get; protected set; } = null!;

    public static RepeatWeekday New(RepeatWeekday value)
    {
        return new RepeatWeekday
        {
            Weekdays = value.Weekdays.Select(x => new Weekday(x.Value)).ToArray(),
            TimeOfTheDayInMinutes = value.TimeOfTheDayInMinutes,
            DurationMinutes = value.DurationMinutes,
            TimeZone = value.TimeZone,
            Interval = value.Interval,
        };
    }

    internal static RepeatWeekday New(
        int timeOfTheDayUtcMinutes,
        int durationMinutes,
        Weekday[] weekdays,
        TimeZoneInfo timeZone,
        int? interval = null)
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
            TimeOfTheDayInMinutes = timeOfTheDayUtcMinutes,
            TimeZone = timeZone,
            Interval = interval,
        };
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        return new object?[] { TimeOfTheDayInMinutes, DurationMinutes, Weekdays, TimeZone, Interval }.Concat(Weekdays);
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