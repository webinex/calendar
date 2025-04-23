namespace Webinex.Calendar.MicrosoftGraph;

/// <summary>
///     Specifies recurrence pattern for the <see cref="MGRecurrence"/>.
/// </summary>
public class MGRecurrencePattern : Equatable
{
    private readonly List<DayOfWeek> _daysOfWeek = new();

    public RecurrenceType Type { get; protected init; }
    public int Interval { get; protected init; }

    public IReadOnlyCollection<DayOfWeek>? DaysOfWeek =>
        Type != RecurrenceType.Weekly && Type != RecurrenceType.RelativeMonthly &&
        Type != RecurrenceType.RelativeYearly
            ? null
            : _daysOfWeek;

    public DayOfWeek? FirstDayOfWeek { get; protected init; }
    public int? DayOfMonth { get; protected init; }
    public int? Month { get; protected init; }
    public MGRecurrenceRelativeIndex? Index { get; protected init; }

    public MGRecurrencePattern(
        RecurrenceType type,
        int interval = 1,
        IReadOnlyCollection<DayOfWeek>? daysOfWeek = null,
        DayOfWeek? firstDayOfWeek = null,
        int? dayOfMonth = null,
        int? month = null,
        MGRecurrenceRelativeIndex? index = null)
    {
        daysOfWeek ??= new List<DayOfWeek>();
        Guard.Gte(interval, 1);

        switch (type)
        {
            case RecurrenceType.Daily:
                Guard.Empty(daysOfWeek).Null(firstDayOfWeek).Null(dayOfMonth).Null(month).Null(index);
                break;
            case RecurrenceType.Weekly:
                Guard.Null(dayOfMonth).Null(month).Null(index);
                break;
            case RecurrenceType.AbsoluteMonthly:
            {
                Guard.NotNull(dayOfMonth).Empty(daysOfWeek).Null(firstDayOfWeek).Null(month).Null(index);
                break;
            }
            case RecurrenceType.RelativeMonthly:
            {
                Guard
                    .NotEmpty(daysOfWeek).EnumDefined(daysOfWeek)
                    .EnumDefined(firstDayOfWeek)
                    .NotNull(index).EnumDefined(index)
                    .Null(dayOfMonth)
                    .Null(month);
                break;
            }
            case RecurrenceType.AbsoluteYearly:
            {
                Guard
                    .NotNull(dayOfMonth).Gte(dayOfMonth, 1).Lte(dayOfMonth, 31)
                    .NotNull(month).Gte(month, 1).Lte(month, 12)
                    .Empty(daysOfWeek)
                    .Null(firstDayOfWeek)
                    .Null(index);
                break;
            }
            case RecurrenceType.RelativeYearly:
            {
                Guard
                    .NotEmpty(daysOfWeek).EnumDefined(daysOfWeek)
                    .EnumDefined(firstDayOfWeek)
                    .NotNull(month).Gte(month, 1).Lte(month, 12)
                    .NotNull(index).EnumDefined(index)
                    .Null(dayOfMonth);
                break;
            }
            default:
                throw new ArgumentException("Unknown recurrence type", nameof(type));
        }

        Type = type;
        Interval = interval;
        _daysOfWeek.AddRange(daysOfWeek);
        FirstDayOfWeek = firstDayOfWeek;
        DayOfMonth = dayOfMonth;
        Month = month;
        Index = index;
    }

    internal MGRecurrencePattern(MGRecurrencePattern value) : this(
        value.Type,
        value.Interval,
        value.DaysOfWeek?.ToArray(),
        value.FirstDayOfWeek,
        value.DayOfMonth,
        value.Month,
        value.Index)
    {
    }

    protected MGRecurrencePattern()
    {
    }

    public static MGRecurrencePattern Daily(int interval = 1)
    {
        return new MGRecurrencePattern(RecurrenceType.Daily, interval: interval);
    }

    public static MGRecurrencePattern Weekly(
        IEnumerable<DayOfWeek> daysOfWeek,
        int interval = 1,
        DayOfWeek? firstDayOfWeek = null)
    {
        return new MGRecurrencePattern(
            RecurrenceType.Weekly,
            daysOfWeek: daysOfWeek.ToArray(),
            interval: interval,
            firstDayOfWeek: firstDayOfWeek);
    }

    public static MGRecurrencePattern AbsoluteMonthly(int dayOfMonth, int interval = 1)
    {
        return new MGRecurrencePattern(RecurrenceType.AbsoluteMonthly, dayOfMonth: dayOfMonth, interval: interval);
    }

    public static MGRecurrencePattern RelativeMonthly(
        IEnumerable<DayOfWeek> daysOfWeek,
        MGRecurrenceRelativeIndex index,
        int interval = 1,
        DayOfWeek? firstDayOfWeek = null)
    {
        return new MGRecurrencePattern(
            RecurrenceType.RelativeMonthly,
            daysOfWeek: daysOfWeek.ToArray(),
            interval: interval,
            firstDayOfWeek: firstDayOfWeek);
    }

    public static MGRecurrencePattern AbsoluteYearly(int dayOfMonth, int month, int interval = 1)
    {
        return new MGRecurrencePattern(
            RecurrenceType.AbsoluteYearly,
            dayOfMonth: dayOfMonth,
            month: month,
            interval: interval);
    }

    public static MGRecurrencePattern RelativeYearly(
        IEnumerable<DayOfWeek> daysOfWeek,
        int month,
        MGRecurrenceRelativeIndex index,
        int interval = 1,
        DayOfWeek? firstDayOfWeek = null)
    {
        return new MGRecurrencePattern(
            RecurrenceType.RelativeYearly,
            daysOfWeek: daysOfWeek.ToArray(),
            month: month,
            interval: interval,
            firstDayOfWeek: firstDayOfWeek,
            index: index);
    }

    public MGRecurrencePattern Clone() => new(this);

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Type;
        yield return Interval;
        yield return DaysOfWeek;
        yield return FirstDayOfWeek;
        yield return DayOfMonth;
        yield return Month;
        yield return Index;
    }
}