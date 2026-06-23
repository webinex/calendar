using Ical.Net;
using Ical.Net.CalendarComponents;
using Ical.Net.DataTypes;
using NodaTime;
using NodaTime.Extensions;
using Webinex.Calendar.Extensions;

namespace Webinex.Calendar.Calculators;

public static class RecurrenceCalculator
{
    public static IEnumerable<Period<DateTimeOffset>> Occurrences(
        IEvent @event,
        OpenPeriod<DateTimeOffset> searchWindow)
    {
        if (@event.Recurrence == null)
            throw new InvalidOperationException(
                "Cannot calculate occurrences for a non-recurring event. Recurrence configuration is required.");

        var searchWindowZoned = searchWindow.InZone(@event.TimeZone);
        var minStartOfOccurrence = MinStartOfOccurrenceInSearchWindow(@event, searchWindowZoned);
        var isSearchWindowOutOfStartOfOccurrences = searchWindowZoned.End.HasValue &&
                                                    minStartOfOccurrence.CompareTo(searchWindowZoned.End.Value) >= 0;

        if (isSearchWindowOutOfStartOfOccurrences)
            return [];

        var occurrences = GetICalOccurrenceEnumerable(@event, minStartOfOccurrence, searchWindowZoned.End);
        return occurrences.Select(x => Map(@event, x)).Where(searchWindow.Intersects);
    }

    private static LocalDateTime MinStartOfOccurrenceInSearchWindow(
        IEvent @event,
        OpenPeriod<LocalDateTime> searchWindowZoned)
    {
        var recurrenceStartZoned = @event.Recurrence!.MGRecurrence!.Period.Start.ToLocalDateTime();
        return LocalDateTime.Max(searchWindowZoned.Start, recurrenceStartZoned);
    }

    private static IEnumerable<Occurrence> GetICalOccurrenceEnumerable(
        IEvent @event,
        LocalDateTime start,
        LocalDateTime? end)
    {
        var iCalEvent = MapToICalEvent(@event);

        var takeWhile = end?.ToDateTimeUnspecified().AsUTCCalDateTime()
                        ?? iCalEvent.RecurrenceRule?.Until
                        ?? new CalDateTime(CalendarConstants.MAX_DATE_TIME, TimeZoneInfo.Utc.Id);

        // We use .AddSeconds(-1) to avoid match events by inclusive end
        // For Ical period of 18:00 - 19:00 will match with event which starts at 19:00
        takeWhile = takeWhile.AddSeconds(-1);

        var calendar = new Ical.Net.Calendar();
        calendar.Events.Add(iCalEvent);

        return calendar.GetOccurrences(start.ToDateTimeUnspecified().AsUTCCalDateTime())
            .TakeWhileBefore(takeWhile);
    }

    private static CalendarEvent MapToICalEvent(IEvent @event)
    {
        var eventPeriodZoned = @event.Period.InZone(@event.TimeZone);
        var eventRecurrencePeriodEndZoned =
            @event.Recurrence!.MGRecurrence!.Period.End?.InZone(@event.TimeZone).ToDateTimeUnspecified();
        var endZoned = eventRecurrencePeriodEndZoned?.AddDays(1) ?? CalendarConstants.MAX_DATE_TIME.AddDays(1);

        var pattern = MGRecurrencePatternConverter.ConvertToIcal(@event.Recurrence!.MGRecurrence!.Pattern, endZoned);

        return new CalendarEvent
        {
            // We use UTC, because we want to remove all timezone manipulations from ICal.Net.
            // We only need to get times, so to do that we work with UTC timezone and then in Map convert to actual timezone
            Start = new CalDateTime(eventPeriodZoned.Start.ToDateTimeUnspecified(), "UTC"),
            End = new CalDateTime(eventPeriodZoned.End.ToDateTimeUnspecified(), "UTC"),
            RecurrenceRule = pattern,
        };
    }

    private static Period<DateTimeOffset> Map(IEvent @event, Occurrence x)
    {
        var dtTz = DateTimeZoneProviders.Tzdb[@event.TimeZone];
        // We don't care about timezone of x.Period.StartTime, because we have TimeZoneinfo in @event.At.TimeZone
        // Here we just need to get times from occurrences and convert then to TimeZone times
        var start = x.Period.StartTime.Value.ToLocalDateTime().InZoneLeniently(dtTz).ToDateTimeOffset()
            .ToUniversalTime();
        var end = start.Add(@event.Period.Duration());

        return new Period<DateTimeOffset>(start, end);
    }
}
