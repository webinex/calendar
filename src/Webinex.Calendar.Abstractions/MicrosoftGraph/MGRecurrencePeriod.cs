namespace Webinex.Calendar.MicrosoftGraph;

/// <summary>
///     NOTE: Start and End date are inclusive
/// </summary>
public class MGRecurrencePeriod : OpenPeriod<DateOnly>
{
    public int? NumberOfOccurrences { get; protected set; }

    public MGRecurrencePeriod(
        DateOnly start,
        DateOnly? end,
        int? numberOfOccurrences = null)
        : base(
            Guard.Arg(start).Lt(CalendarConstants.MAX_DATE_ONLY).Value,
            Guard.Arg(end).Lt(CalendarConstants.MAX_DATE_ONLY).Value)
    {
        NumberOfOccurrences = Guard.Arg(numberOfOccurrences).Gt(0).Value;
    }

    internal MGRecurrencePeriod(MGRecurrencePeriod value)
    {
        value = value ?? throw new ArgumentNullException(nameof(value));
        Start = value.Start;
        End = value.End;
        NumberOfOccurrences = value.NumberOfOccurrences;
    }

    protected MGRecurrencePeriod()
    {
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        return base.GetEqualityComponents().Concat([NumberOfOccurrences]);
    }

    public MGRecurrencePeriod WithEnd(DateOnly? value)
    {
        return new MGRecurrencePeriod(this)
        {
            End = Guard.Arg(value).Lt(CalendarConstants.MAX_DATE_ONLY).Value,
        };
    }

    public MGRecurrencePeriod WithStart(DateOnly value)
    {
        return new MGRecurrencePeriod(this)
        {
            Start = Guard.Arg(value).Lt(CalendarConstants.MAX_DATE_ONLY).Value,
        };
    }

    public new MGRecurrencePeriod Clone() => new(this);
}