using Webinex.Calendar.Common;

namespace Webinex.Calendar.Repeats;

public class Repeat : Equatable
{
    protected Repeat()
    {
    }

    public RepeatInterval? Interval { get; protected set; }
    public RepeatWeekday? Weekday { get; protected set; }
    public RepeatDayOfMonth? DayOfMonth { get; protected set; }

    public static Repeat New(Repeat repeat)
    {
        return new Repeat
        {
            Interval = repeat.Interval != null ? RepeatInterval.New(repeat.Interval) : null,
            Weekday = repeat.Weekday != null ? RepeatWeekday.New(repeat.Weekday) : null,
            DayOfMonth = repeat.DayOfMonth != null ? RepeatDayOfMonth.New(repeat.DayOfMonth) : null,
        };
    }

    public static Repeat NewInterval(
        DateTimeOffset start,
        DateTimeOffset? end,
        int intervalMinutes,
        int durationMinutes)
    {
        return new Repeat
        {
            Interval = RepeatInterval.New(start, end, intervalMinutes, durationMinutes),
        };
    }

    public static Repeat NewInterval(RepeatInterval interval)
    {
        return new Repeat
        {
            Interval = interval,
        };
    }

    public static Repeat NewWeekday(
        int timeOfTheDayUtcMinutes,
        int durationMinutes,
        Weekday[] weekdays)
    {
        return new Repeat
        {
            Weekday = RepeatWeekday.New(timeOfTheDayUtcMinutes, durationMinutes, weekdays),
        };
    }

    public static Repeat NewWeekday(RepeatWeekday weekday)
    {
        return new Repeat
        {
            Weekday = weekday,
        };
    }

    public static Repeat NewDayOfMonth(
        int timeOfTheDayUtcMinutes,
        int durationMinutes,
        DayOfMonth dayOfMonth)
    {
        return new Repeat
        {
            DayOfMonth = RepeatDayOfMonth.New(timeOfTheDayUtcMinutes, durationMinutes, dayOfMonth),
        };
    }

    public static Repeat NewDayOfMonth(RepeatDayOfMonth repeat)
    {
        return new Repeat
        {
            DayOfMonth = repeat,
        };
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Interval;
        yield return Weekday;
        yield return DayOfMonth;
    }
}