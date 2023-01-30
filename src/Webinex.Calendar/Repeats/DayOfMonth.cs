using Webinex.Calendar.Common;

namespace Webinex.Calendar.Repeats;

public class DayOfMonth : ValueObject, ISingleValueObject<int>
{
    protected DayOfMonth()
    {
    }
    
    public DayOfMonth(int value)
    {
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