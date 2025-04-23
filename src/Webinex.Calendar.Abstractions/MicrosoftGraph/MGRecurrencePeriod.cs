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
        : base(start, end)
    {
        NumberOfOccurrences = numberOfOccurrences;
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
            End = value,
        };
    }

    public MGRecurrencePeriod WithStart(DateOnly value)
    {
        return new MGRecurrencePeriod(this)
        {
            Start = value,
        };
    }

    public new MGRecurrencePeriod Clone() => new(this);
}