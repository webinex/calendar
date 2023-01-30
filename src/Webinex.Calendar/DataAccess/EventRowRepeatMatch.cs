using System.Linq.Expressions;
using Webinex.Calendar.Repeats;

namespace Webinex.Calendar.DataAccess;

public class EventRowRepeatMatch
{
    protected EventRowRepeatMatch()
    {
    }

    public int TimeOfTheDayUtcMinutes { get; protected set; }
    public int DurationMinutes { get; protected set; }
    public int? OvernightDurationMinutes { get; protected set; }
    public int SameDayLastTime { get; protected set; }

    public bool Monday { get; protected set; }
    public bool Tuesday { get; protected set; }
    public bool Wednesday  { get; protected set; }
    public bool Thursday  { get; protected set; }
    public bool Friday  { get; protected set; }
    public bool Saturday  { get; protected set; }
    public bool Sunday  { get; protected set; }
    public int? DayOfMonth { get; protected set; }

    public static EventRowRepeatMatch From(RepeatMatch match)
    {
        return new EventRowRepeatMatch
        {
            TimeOfTheDayUtcMinutes = match.TimeOfTheDayUtcMinutes,
            DurationMinutes = match.DurationMinutes,
            OvernightDurationMinutes = match.OvernightDurationMinutes,
            SameDayLastTime = match.SameDayLastTime,
            Monday = match.Weekdays.Contains(Weekday.Monday),
            Tuesday = match.Weekdays.Contains(Weekday.Tuesday),
            Wednesday = match.Weekdays.Contains(Weekday.Wednesday),
            Thursday = match.Weekdays.Contains(Weekday.Thursday),
            Friday = match.Weekdays.Contains(Weekday.Friday),
            Saturday = match.Weekdays.Contains(Weekday.Saturday),
            Sunday = match.Weekdays.Contains(Weekday.Sunday),
            DayOfMonth = match.DayOfMonth?.Value,
        };
    }

    public RepeatMatch ToModel()
    {
        var dayOfMonth = DayOfMonth.HasValue ? new DayOfMonth(DayOfMonth.Value) : null;
        return RepeatMatch.New(TimeOfTheDayUtcMinutes, DurationMinutes, Weekdays(), dayOfMonth);
    }

    private Weekday[] Weekdays()
    {
        var weekdays = new LinkedList<Weekday>();

        if (Monday)
            weekdays.AddLast(Weekday.Monday);

        if (Tuesday)
            weekdays.AddLast(Weekday.Tuesday);

        if (Wednesday)
            weekdays.AddLast(Weekday.Wednesday);

        if (Thursday)
            weekdays.AddLast(Weekday.Thursday);

        if (Friday)
            weekdays.AddLast(Weekday.Friday);

        if (Saturday)
            weekdays.AddLast(Weekday.Saturday);

        if (Sunday)
            weekdays.AddLast(Weekday.Sunday);

        return weekdays.ToArray();
    }
}