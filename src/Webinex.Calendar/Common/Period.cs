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

    public Period ToUtc()
    {
        return new Period(Start.ToUtc(), End.ToUtc());
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