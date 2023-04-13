namespace Webinex.Calendar.Common;

public class OpenPeriod : Equatable
{
    protected OpenPeriod()
    {
    }

    public OpenPeriod(DateTimeOffset start, DateTimeOffset? end)
    {
        Start = start;
        End = end;

        if (end.HasValue && end < start)
        {
            throw new ArgumentException("Might be greater than start", nameof(end));
        }
    }

    public DateTimeOffset Start { get; protected set; }
    public DateTimeOffset? End { get; protected set; }

    public static OpenPeriod New(OpenPeriod value)
    {
        return new OpenPeriod
        {
            Start = value.Start,
            End = value.End,
        };
    }

    public Period ToPeriod()
    {
        if (!End.HasValue)
        {
            throw new InvalidOperationException(
                $"Unable to convert {nameof(OpenPeriod)} to {nameof(Period)}. {nameof(End)} unset");
        }

        return new Period(Start, End.Value);
    }

    public OpenPeriod ToUtc()
    {
        return new OpenPeriod(Start.ToUtc(), End?.ToUtc());
    }

    public static bool operator ==(OpenPeriod? left, OpenPeriod? right)
    {
        return EqualOperator(left, right);
    }

    public static bool operator !=(OpenPeriod? left, OpenPeriod? right)
    {
        return NotEqualOperator(left, right);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Start;
        yield return End;
    }
}