namespace Webinex.Calendar;

public static class Period
{
    public static Period<T> New<T>(T start, T end)
        where T : IComparable<T>
    {
        return new Period<T>(start, end);
    }
}

public class Period<T> : Equatable
    where T : IComparable<T>
{
    protected Period()
    {
    }

    public Period(T start, T end)
    {
        if (start is IComparable startComparable && end is IComparable endComparable &&
            endComparable.CompareTo(startComparable) < 0)
            throw new ArgumentException("Might be greater than start", nameof(end));

        Start = start;
        End = end;
    }

    public T Start { get; protected set; } = default!;
    public T End { get; protected set; } = default!;

    public bool Intersects(Period<T> y)
    {
        return Start.CompareTo(y.End) < 0 && End.CompareTo(y.Start) > 0;
    }

    public static bool operator ==(Period<T>? left, Period<T>? right)
    {
        return EqualOperator(left, right);
    }

    public static bool operator !=(Period<T>? left, Period<T>? right)
    {
        return NotEqualOperator(left, right);
    }

    public override string ToString() => $"{Start:s} - {End:s}";

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Start;
        yield return End;
    }

    public void Deconstruct(out T start, out T end)
    {
        start = Start;
        end = End;
    }
    
    public Period<T> Clone() => new(Start, End);
}