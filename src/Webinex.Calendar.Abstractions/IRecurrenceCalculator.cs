namespace Webinex.Calendar;

public interface IRecurrenceCalculator
{
    IEnumerable<Period<DateTimeOffset>> Occurrences(
        Period<DateTimeOffset> eventPeriod,
        string timeZone,
        Recurrence recurrence,
        OpenPeriod<DateTimeOffset> @in);

    IEnumerable<Period<DateTimeOffset>> Occurrences(
        IEvent @event,
        OpenPeriod<DateTimeOffset> @in);

    Period<DateTimeOffset>? FirstOccurrence(Period<DateTimeOffset> eventPeriod, string timeZone, Recurrence? recurrence);
    Period<DateTimeOffset>? FirstOccurrence(IEvent @event);
}