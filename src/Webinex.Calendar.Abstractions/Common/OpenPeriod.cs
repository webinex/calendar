namespace Webinex.Calendar;

public class OpenPeriod<T> : Equatable
    where T : struct, IComparable<T>
{
    protected OpenPeriod()
    {
    }

    public OpenPeriod(T start, T? end)
    {
        Start = start;
        End = end;

        if (end != null && end.Value.CompareTo(start) < 0)
            throw new ArgumentException("Might be greater than or equal to start", nameof(end));
    }

    public T Start { get; protected set; } = default!;
    public T? End { get; protected set; }

    public bool Intersects(Period<T> period)
    {
        if (End.HasValue)
            return ToPeriod().Intersects(period);

        return period.End.CompareTo(Start) > 0;
    }

    public static OpenPeriod<T> New(OpenPeriod<T> value)
    {
        return new OpenPeriod<T>
        {
            Start = value.Start,
            End = value.End,
        };
    }

    public Period<T> ToPeriod()
    {
        if (End == null)
        {
            throw new InvalidOperationException(
                $"Unable to convert {nameof(OpenPeriod<T>)} to {nameof(Period<T>)}. {nameof(End)} unset");
        }

        return new Period<T>(Start, End.Value);
    }

    public static bool operator ==(OpenPeriod<T>? left, OpenPeriod<T>? right)
    {
        return EqualOperator(left, right);
    }

    public static bool operator !=(OpenPeriod<T>? left, OpenPeriod<T>? right)
    {
        return NotEqualOperator(left, right);
    }

    public override string ToString() => $"{Start}-{End}";

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Start;
        yield return End;
    }
    
    public OpenPeriod<T> Clone() => new(Start, End);
}