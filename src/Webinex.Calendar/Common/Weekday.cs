namespace Webinex.Calendar.Common;

public class Weekday : Equatable
{
    private static readonly Weekday[] ORDERED_VALUES =
    {
        Monday, Tuesday, Wednesday, Thursday, Friday, Saturday, Sunday,
    };

    private static readonly HashSet<string> POSSIBLE_VALUES = new(ORDERED_VALUES.Select(x => x.Value));

    protected Weekday()
    {
    }

    public Weekday(string value)
    {
        Value = value;
    }

    public string Value { get; protected init; } = null!;

    public static Weekday Monday => new() { Value = "Monday" };
    public static Weekday Tuesday => new() { Value = "Tuesday" };
    public static Weekday Wednesday => new() { Value = "Wednesday" };
    public static Weekday Thursday => new() { Value = "Thursday" };
    public static Weekday Friday => new() { Value = "Friday" };
    public static Weekday Saturday => new() { Value = "Saturday" };
    public static Weekday Sunday => new() { Value = "Sunday" };

    public static Weekday[] All => POSSIBLE_VALUES.Select(value => new Weekday { Value = value }).ToArray();

    public static bool operator ==(Weekday? left, Weekday? right)
    {
        return EqualOperator(left, right);
    }

    public static bool operator !=(Weekday? left, Weekday? right)
    {
        return NotEqualOperator(left, right);
    }

    public Weekday Next()
    {
        return Add(1);
    }

    public Weekday Previous()
    {
        return Add(-1);
    }

    public Weekday Add(int days)
    {
        var currentOrderIndex = Array.IndexOf(ORDERED_VALUES, this);
        var nextOrderIndex = (currentOrderIndex + days) % ORDERED_VALUES.Length;
        nextOrderIndex = nextOrderIndex < 0 ? ORDERED_VALUES.Length + nextOrderIndex : nextOrderIndex;
        return ORDERED_VALUES.ElementAt(nextOrderIndex);
    }

    public static Weekday From(DayOfWeek dayOfWeek)
    {
        return dayOfWeek switch
        {
            DayOfWeek.Sunday => Sunday,
            DayOfWeek.Monday => Monday,
            DayOfWeek.Tuesday => Tuesday,
            DayOfWeek.Wednesday => Wednesday,
            DayOfWeek.Thursday => Thursday,
            DayOfWeek.Friday => Friday,
            DayOfWeek.Saturday => Saturday,
            _ => throw new ArgumentOutOfRangeException(nameof(dayOfWeek), dayOfWeek, "Not found"),
        };
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }
}