using Webinex.Calendar.MicrosoftGraph;

namespace Webinex.Calendar;

public class Recurrence : Equatable
{
    /// <summary>
    ///     Microsoft Graph API recurrence rules
    /// </summary>
    public MGRecurrence? MGRecurrence { get; protected init; } = null!;

    public Recurrence(MGRecurrence? mgRecurrence)
    {
        MGRecurrence = mgRecurrence?.Clone() ?? throw new ArgumentNullException(nameof(mgRecurrence));
    }

    internal Recurrence(Recurrence value)
    {
        value = value ?? throw new ArgumentNullException(nameof(value));
        MGRecurrence = value.MGRecurrence?.Clone();
    }

    protected Recurrence()
    {
    }

    public Recurrence Clone() => new(this);

    public Recurrence WithEnd(DateOnly value)
    {
        return new Recurrence(this)
        {
            MGRecurrence = MGRecurrence?.WithEnd(value),
        };
    }

    public Recurrence WithStart(DateOnly value)
    {
        return new Recurrence(this)
        {
            MGRecurrence = MGRecurrence?.WithStart(value),
        };
    }

    public Recurrence WithPeriod(DateOnly start, DateOnly? end)
    {
        return new Recurrence(this)
        {
            MGRecurrence = MGRecurrence?.WithPeriod(start, end),
        };
    }

    public DateOnly? EndDate() => MGRecurrence?.Period.End;
    public DateOnly StartDate() => MGRecurrence!.Period.Start;
    
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return MGRecurrence;
    }
}