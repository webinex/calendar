namespace Webinex.Calendar.Common;

public class DayOfMonth : Equatable
{
    protected DayOfMonth()
    {
    }

    public DayOfMonth(int value)
    {
        if (value < 0 || value > 31)
        {
            throw new ArgumentException(
                $"Might be greater than 0 and less than or equal to 31. Actual value: {value}",
                nameof(value));
        }

        Value = value;
    }

    public int Value { get; protected set; }

    public static bool operator ==(DayOfMonth? left, DayOfMonth? right)
    {
        return EqualOperator(left, right);
    }

    public static bool operator !=(DayOfMonth? left, DayOfMonth? right)
    {
        return NotEqualOperator(left, right);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public int Convert()
    {
        return Value;
    }
}