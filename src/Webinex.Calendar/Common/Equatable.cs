namespace Webinex.Calendar.Common;

public abstract class Equatable
{
    protected abstract IEnumerable<object?> GetEqualityComponents();

    public override bool Equals(object? obj)
    {
        if (obj == null || obj.GetType() != GetType())
        {
            return false;
        }

        var other = (Equatable)obj;

        return GetEqualityComponents().SequenceEqual(other.GetEqualityComponents());
    }

    public override int GetHashCode()
    {
        return GetEqualityComponents()
            .Select(x => x?.GetHashCode() ?? 0)
            .Aggregate((x, y) => x ^ y);
    }

    public static bool operator ==(Equatable? left, Equatable? right)
    {
        return EqualOperator(left, right);
    }

    public static bool operator !=(Equatable? left, Equatable? right)
    {
        return NotEqualOperator(left, right);
    }

    protected static bool EqualOperator(Equatable? left, Equatable? right)
    {
        if (ReferenceEquals(left, null) ^ ReferenceEquals(right, null))
        {
            return false;
        }

        return ReferenceEquals(left, null) || left.Equals(right);
    }

    protected static bool NotEqualOperator(Equatable? left, Equatable? right)
    {
        return !EqualOperator(left, right);
    }
}