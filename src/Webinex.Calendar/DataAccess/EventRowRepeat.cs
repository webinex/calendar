using Webinex.Calendar.Common;
using Webinex.Calendar.Repeats;

namespace Webinex.Calendar.DataAccess;

public class EventRowRepeat
{
    protected EventRowRepeat()
    {
    }

    public EventRowRepeatType Type { get; protected set; }
    public int? IntervalMinutes { get; protected set; }
    public int DurationMinutes { get; protected set; }
    public int TimeOfTheDayUtcMinutes { get; protected set; }
    public int? OvernightDurationMinutes { get; protected set; }
    public int? SameDayLastTime { get; protected set; }

    public bool? Monday { get; protected set; }
    public bool? Tuesday { get; protected set; }
    public bool? Wednesday { get; protected set; }
    public bool? Thursday { get; protected set; }
    public bool? Friday { get; protected set; }
    public bool? Saturday { get; protected set; }
    public bool? Sunday { get; protected set; }

    public int? DayOfMonth { get; protected set; }

    private Weekday[] Weekdays => new[]
    {
        Monday == true ? Weekday.Monday : null,
        Tuesday == true ? Weekday.Tuesday : null,
        Wednesday == true ? Weekday.Wednesday : null,
        Thursday == true ? Weekday.Thursday : null,
        Friday == true ? Weekday.Friday : null,
        Saturday == true ? Weekday.Saturday : null,
        Sunday == true ? Weekday.Sunday : null,
    }.Where(x => x != null).Cast<Weekday>().ToArray();

    internal static EventRowRepeat From(Repeat repeat)
    {
        if (repeat.Interval != null)
            return From(repeat.Interval);

        if (repeat.DayOfMonth != null)
            return From(repeat.DayOfMonth);

        if (repeat.Weekday != null)
            return From(repeat.Weekday);

        throw new ArgumentException(nameof(repeat));
    }

    private static EventRowRepeat From(RepeatInterval repeat)
    {
        return new EventRowRepeat
        {
            Type = EventRowRepeatType.Interval,
            IntervalMinutes = repeat.IntervalMinutes,
            DurationMinutes = repeat.DurationMinutes,
            SameDayLastTime = repeat.SameDayLastTime(),
            TimeOfTheDayUtcMinutes = repeat.TimeOfTheDayUtcMinutes,
            OvernightDurationMinutes = repeat.OvernightMinutes(),
        };
    }

    private static EventRowRepeat From(RepeatDayOfMonth repeat)
    {
        return new EventRowRepeat
        {
            Type = EventRowRepeatType.DayOfMonth,
            DurationMinutes = repeat.DurationMinutes,
            SameDayLastTime = repeat.SameDayLastTime(),
            TimeOfTheDayUtcMinutes = repeat.TimeOfTheDayUtcMinutes,
            OvernightDurationMinutes = repeat.OvernightMinutes(),
            DayOfMonth = repeat.DayOfMonth.Value,
        };
    }

    private static EventRowRepeat From(RepeatWeekday repeat)
    {
        return new EventRowRepeat
        {
            Type = EventRowRepeatType.Weekday,
            DurationMinutes = repeat.DurationMinutes,
            SameDayLastTime = repeat.SameDayLastTime(),
            TimeOfTheDayUtcMinutes = repeat.TimeOfTheDayUtcMinutes,
            OvernightDurationMinutes = repeat.OvernightMinutes(),
            Monday = repeat.Weekdays.Contains(Weekday.Monday),
            Tuesday = repeat.Weekdays.Contains(Weekday.Tuesday),
            Wednesday = repeat.Weekdays.Contains(Weekday.Wednesday),
            Thursday = repeat.Weekdays.Contains(Weekday.Thursday),
            Friday = repeat.Weekdays.Contains(Weekday.Friday),
            Saturday = repeat.Weekdays.Contains(Weekday.Saturday),
            Sunday = repeat.Weekdays.Contains(Weekday.Sunday),
        };
    }

    internal Repeat ToModel(OpenPeriodMinutesSince1990 period)
    {
        return Type switch
        {
            EventRowRepeatType.Interval => ToIntervalModel(period),
            EventRowRepeatType.Weekday => ToWeekdayModel(),
            EventRowRepeatType.DayOfMonth => ToDayOfMonthModel(),
            _ => throw new InvalidOperationException($"Unknown type {Type:G}"),
        };
    }

    internal Repeat ToIntervalModel(OpenPeriodMinutesSince1990 period)
    {
        return Repeat.NewInterval(
            period.ToOpenPeriod().Start,
            period.ToOpenPeriod().End,
            IntervalMinutes!.Value,
            DurationMinutes);
    }

    internal Repeat ToWeekdayModel()
    {
        return Repeat.NewWeekday(TimeOfTheDayUtcMinutes, DurationMinutes, Weekdays);
    }

    internal Repeat ToDayOfMonthModel()
    {
        return Repeat.NewDayOfMonth(TimeOfTheDayUtcMinutes, DurationMinutes, new DayOfMonth(DayOfMonth!.Value));
    }
}