using Webinex.Calendar.Common;

namespace Webinex.Calendar.Repeats;

public class Repeat : ValueObject
{
    protected Repeat()
    {
    }

    public RepeatInterval? Interval { get; protected set; }
    public RepeatMatch? Match { get; protected set; }

    public static Repeat NewInterval(DateTimeOffset start, DateTimeOffset? end, int intervalMinutes, int durationMinutes)
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

    public static Repeat NewMatch(
        int timeOfTheDayUtcMinutes,
        int durationMinutes,
        Weekday[] weekdays,
        DayOfMonth? dayOfMonth)
    {
        return new Repeat
        {
            Match = RepeatMatch.New(timeOfTheDayUtcMinutes, durationMinutes, weekdays, dayOfMonth),
        };
    }

    public static Repeat NewMatch(RepeatMatch match)
    {
        return new Repeat
        {
            Match = match,
        };
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Interval;
        yield return Match;
    }
}