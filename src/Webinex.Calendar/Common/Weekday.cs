namespace Webinex.Calendar.Common;

public class Weekday : Equatable
{
    public const int DAYS_IN_WEEK = 7;

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

    private const string MONDAY = "Monday";
    private const string TUESDAY = "Tuesday";
    private const string WEDNESDAY = "Wednesday";
    private const string THURSDAY = "Thursday";
    private const string FRIDAY = "Friday";
    private const string SATURDAY = "Saturday";
    private const string SUNDAY = "Sunday";

    public static Weekday Monday => new() { Value = MONDAY };
    public static Weekday Tuesday => new() { Value = TUESDAY };
    public static Weekday Wednesday => new() { Value = WEDNESDAY };
    public static Weekday Thursday => new() { Value = THURSDAY };
    public static Weekday Friday => new() { Value = FRIDAY };
    public static Weekday Saturday => new() { Value = SATURDAY };
    public static Weekday Sunday => new() { Value = SUNDAY };

    public static Weekday[] All => POSSIBLE_VALUES.Select(value => new Weekday { Value = value }).ToArray();

    public Weekday Next() => Add(1);
    public Weekday Previous() => Add(-1);

    private int OrderIndex() => Value switch
    {
        MONDAY => 0,
        TUESDAY => 1,
        WEDNESDAY => 2,
        THURSDAY => 3,
        FRIDAY => 4,
        SATURDAY => 5,
        SUNDAY => 6,
        _ => throw new ArgumentOutOfRangeException(),
    };

    public Weekday Add(int days)
    {
        var nextOrderIndex = (OrderIndex() + days) % DAYS_IN_WEEK;
        nextOrderIndex = nextOrderIndex < 0 ? DAYS_IN_WEEK + nextOrderIndex : nextOrderIndex;
        return ORDERED_VALUES[nextOrderIndex];
    }

    public static Weekday From(DayOfWeek dayOfWeek) => dayOfWeek switch
    {
        DayOfWeek.Monday => Weekday.Monday,
        DayOfWeek.Tuesday => Weekday.Tuesday,
        DayOfWeek.Wednesday => Weekday.Wednesday,
        DayOfWeek.Thursday => Weekday.Thursday,
        DayOfWeek.Friday => Weekday.Friday,
        DayOfWeek.Saturday => Weekday.Saturday,
        DayOfWeek.Sunday => Weekday.Sunday,
        _ => throw new ArgumentOutOfRangeException(),
    };

    public DayOfWeek ToDayOfWeek() => Value switch
    {
        MONDAY => DayOfWeek.Monday,
        TUESDAY => DayOfWeek.Tuesday,
        WEDNESDAY => DayOfWeek.Wednesday,
        THURSDAY => DayOfWeek.Thursday,
        FRIDAY => DayOfWeek.Friday,
        SATURDAY => DayOfWeek.Saturday,
        SUNDAY => DayOfWeek.Sunday,
        _ => throw new ArgumentOutOfRangeException(),
    };
    
    public static bool operator ==(Weekday? left, Weekday? right)
    {
        return EqualOperator(left, right);
    }

    public static bool operator !=(Weekday? left, Weekday? right)
    {
        return NotEqualOperator(left, right);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }
}