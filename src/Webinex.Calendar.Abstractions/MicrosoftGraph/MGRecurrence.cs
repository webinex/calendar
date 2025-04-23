namespace Webinex.Calendar.MicrosoftGraph;

public class MGRecurrence : Equatable
{
    public MGRecurrencePeriod Period { get; protected set; } = null!;
    public MGRecurrencePattern Pattern { get; protected set; } = null!;

    protected MGRecurrence()
    {
    }

    public MGRecurrence(MGRecurrencePeriod period, MGRecurrencePattern pattern)
    {
        Period = period?.Clone() ?? throw new ArgumentNullException(nameof(period));
        Pattern = pattern?.Clone() ?? throw new ArgumentNullException(nameof(pattern));
    }

    internal MGRecurrence(MGRecurrence value)
    {
        value = value ?? throw new ArgumentNullException(nameof(value));
        Period = value.Period.Clone();
        Pattern = value.Pattern.Clone();
    }

    public MGRecurrence Clone() => new(this);

    public MGRecurrence WithEnd(DateOnly value)
    {
        return new MGRecurrence(this)
        {
            Period = Period.WithEnd(value),
        };
    }

    public MGRecurrence WithStart(DateOnly value)
    {
        return new MGRecurrence(this)
        {
            Period = Period.WithStart(value),
        };
    }

    public MGRecurrence WithPeriod(DateOnly start, DateOnly? end)
    {
        return new MGRecurrence(this)
        {
            Period = new MGRecurrencePeriod(start, end),
        };
    }
    
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Period;
        yield return Pattern;
    }
}