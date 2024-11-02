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
        var start = Start.TimeOfDay > TimeSpan.Zero
            ? Start.AddDays(1).StartOfTheDay()
            : Start;

        var end = End.StartOfTheDay();

        if (start >= end) return Array.Empty<Weekday>();

        var diffInDays = (int)(end - start).TotalDays;
        var startWeekday = Weekday.From(start.DayOfWeek);

        return diffInDays switch
        {
            0 => Array.Empty<Weekday>(),
            > 0 and < 7 => Enumerable.Range(0, diffInDays).Select(i => startWeekday.Add(i)).ToArray(),
            >= 7 => Weekday.All,
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    public static bool operator ==(Period? left, Period? right)
    {
        return EqualOperator(left, right);
    }

    public static bool operator !=(Period? left, Period? right)
    {
        return NotEqualOperator(left, right);
    }

    public override string ToString() => $"{Start:s} - {End:s}";

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Start;
        yield return End;
    }
}