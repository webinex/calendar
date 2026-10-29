using Webinex.Calendar.Calculators;

namespace Webinex.Calendar;

internal class RecurrenceCalculatorInstance : IRecurrenceCalculator
{
    public IEnumerable<Period<DateTimeOffset>> Occurrences(
        Period<DateTimeOffset> eventPeriod,
        string timeZone,
        Recurrence recurrence,
        OpenPeriod<DateTimeOffset> @in)
    {
        return RecurrenceCalculator.Occurrences(eventPeriod, timeZone, recurrence, @in);
    }

    public IEnumerable<Period<DateTimeOffset>> Occurrences(
        IEvent @event,
        OpenPeriod<DateTimeOffset> @in)
    {
        return RecurrenceCalculator.Occurrences(@event, @in);
    }

    public Period<DateTimeOffset>? FirstOccurrence(
        Period<DateTimeOffset> eventPeriod,
        string timeZone,
        Recurrence? recurrence)
    {
        return RecurrenceCalculator.FirstOccurrence(eventPeriod, timeZone, recurrence);
    }

    public Period<DateTimeOffset>? FirstOccurrence(IEvent @event)
    {
        return RecurrenceCalculator.FirstOccurrence(@event);
    }
}