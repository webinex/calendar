using Webinex.Calendar.Common;

namespace Webinex.Calendar.Repeats;

public class Weekday : EnumValue<string>
{
    protected Weekday()
    {
    }

    public Weekday(string value) : base(value)
    {
    }

    public static Weekday Monday => new Weekday { Value = "Monday" };
    public static Weekday Tuesday => new Weekday { Value = "Tuesday" };
    public static Weekday Wednesday => new Weekday { Value = "Wednesday" };
    public static Weekday Thursday => new Weekday { Value = "Thursday" };
    public static Weekday Friday => new Weekday { Value = "Friday" };
    public static Weekday Saturday => new Weekday { Value = "Saturday" };
    public static Weekday Sunday => new Weekday { Value = "Sunday" };

    public static bool operator ==(Weekday? left, Weekday? right)
    {
        return EqualOperator(left, right);
    }

    public static bool operator !=(Weekday? left, Weekday? right)
    {
        return NotEqualOperator(left, right);
    }

    private static readonly Weekday[] ORDERED_VALUES = new[]
    {
        Monday, Tuesday, Wednesday, Thursday, Friday, Saturday, Sunday,
    };

    private static readonly HashSet<string> POSSIBLE_VALUES = new(ORDERED_VALUES.Select(x => x.Value));

    public static Weekday[] All => POSSIBLE_VALUES.Select(value => new Weekday { Value = value }).ToArray();

    protected override HashSet<string> PossibleValues => POSSIBLE_VALUES;

    public Weekday Next() => Add(1);

    public Weekday Previous() => Add(-1);

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
}