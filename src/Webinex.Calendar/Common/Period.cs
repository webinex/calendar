namespace Webinex.Calendar.Common;

public class Period : Equatable
{
    protected Period()
    {
    }

    public Period(DateTimeOffset start, DateTimeOffset end)
    {
        if (end < start)
            throw new ArgumentException("Might be greater than start", nameof(end));

        Start = start;
        End = end;
    }

    public DateTimeOffset Start { get; protected set; }
    public DateTimeOffset End { get; protected set; }

    public bool Intersects(Period period)
    {
        return Start < period.End && End > period.Start;
    }

    public Period ToUtc()
    {
        return new Period(Start.ToUtc(), End.ToUtc());
    }

    public Weekday[] FullDayWeekdays()
    {
        var value = Start.TimeOfDay > TimeSpan.Zero
            ? Start.AddDays(1).StartOfTheDay()
            : Start;

        var end = End.StartOfTheDay();

        var weekdays = new LinkedList<Weekday>();
        while (value < end && weekdays.Count < 7)
        {
            weekdays.AddLast(Weekday.From(value.DayOfWeek));
            value = value.AddDays(1);
        }

        return weekdays.Distinct().ToArray();
    }

    public static bool operator ==(Period? left, Period? right)
    {
        return EqualOperator(left, right);
    }

    public static bool operator !=(Period? left, Period? right)
    {
        return NotEqualOperator(left, right);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Start;
        yield return End;
    }
}