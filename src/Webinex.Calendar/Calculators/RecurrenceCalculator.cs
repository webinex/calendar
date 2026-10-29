using Ical.Net.CalendarComponents;
using Ical.Net.DataTypes;
using NodaTime;
using NodaTime.Extensions;
using Webinex.Calendar.Extensions;

namespace Webinex.Calendar.Calculators;

public static class RecurrenceCalculator
{
    // Limits search when recurrence never matches (e.g. yearly on February 30), 29 February matches at least once in 8 years
    private const int FIRST_OCCURRENCE_SEARCH_YEARS = 10;

    public static Period<DateTimeOffset>? FirstOccurrence(IEvent @event)
    {
        if (@event.Recurrence == null)
            return @event.Period;

        return FirstOccurrence(@event.Period, @event.TimeZone, @event.Recurrence);
    }

    /// <summary>
    ///     First occurrence of recurrent event. Event start might not match recurrence
    ///     (e.g. Tuesday with Monday and Wednesday days of week), then first occurrence is later.
    ///     Returns null when recurrence has no occurrences (e.g. ends before first matching day).
    /// </summary>
    public static Period<DateTimeOffset>? FirstOccurrence(
        Period<DateTimeOffset> eventPeriod,
        string timeZone,
        Recurrence? recurrence)
    {
        return Occurrences(eventPeriod, timeZone, recurrence,
                new OpenPeriod<DateTimeOffset>(eventPeriod.Start,
                    eventPeriod.Start.AddYears(FIRST_OCCURRENCE_SEARCH_YEARS)))
            .FirstOrDefault();
    }

    public static IEnumerable<Period<DateTimeOffset>> Occurrences(
        IEvent @event,
        OpenPeriod<DateTimeOffset> @in)
    {
        return Occurrences(@event.Period, @event.TimeZone, @event.Recurrence!, @in);
    }

    public static IEnumerable<Period<DateTimeOffset>> Occurrences(
        Period<DateTimeOffset> eventPeriod,
        string timeZone,
        Recurrence? recurrence,
        OpenPeriod<DateTimeOffset> @in)
    {
        if (recurrence == null)
            return @in.Intersects(eventPeriod) ? new[] { eventPeriod } : Array.Empty<Period<DateTimeOffset>>();
        
        var eventPeriodZoned = eventPeriod.InZone(timeZone);
        var inZoned = @in.InZone(timeZone);
        // Search start shifted one hour back, because occurrence which falls into DST gap
        // (e.g. 02:30 on spring forward day) is moved forward by InZoneLeniently in Map, while ICal.Net keeps
        // original wall time. Without shift such occurrence is out of search range when duration is less than an hour.
        // Extra occurrences are filtered by @in.Intersects below.
        var searchStart = @in.Start > eventPeriod.End
            ? @in.Start.ToUniversalTime().Subtract(eventPeriod.Duration()).AddHours(-1)
            : eventPeriod.Start;

        var endZoned = recurrence.MGRecurrence!.Period.End?.ToDateTime(TimeOnly.MaxValue) ??
                       DateTime.MaxValue;

        var pattern = MGRecurrencePatternConverter.ConvertToIcal(recurrence.MGRecurrence!.Pattern, endZoned);

        var calendarEvent = new CalendarEvent
        {
            // We use UTC, because we want to remove all timezone manipulations from ICal.Net.
            // We only need to get times, so to do that we work with UTC timezone and then in Map convert to actual timezone
            Start = new CalDateTime(eventPeriodZoned.Start.ToDateTimeUnspecified(), "UTC"),
            End = new CalDateTime(eventPeriodZoned.End.ToDateTimeUnspecified(), "UTC"),
            RecurrenceRules = { pattern },
        };

        var calendar = new Ical.Net.Calendar();
        calendar.Events.Add(calendarEvent);

        var occurrences = calendar.GetOccurrencesEnumerable(
            searchStart.ToLocalDateTime(timeZone).ToDateTimeUnspecified(),
            // We use .AddMilliseconds(-1) to avoid match events by inclusive end
            // For Ical period of 18:00 - 19:00 will match with event which starts at 19:00
            inZoned.End?.ToDateTimeUnspecified().AddMilliseconds(-1));

        return occurrences.Select(x => Map(eventPeriod, timeZone, x)).Where(@in.Intersects);
    }

    private static Period<DateTimeOffset> Map(Period<DateTimeOffset> eventPeriod, string timeZone, Occurrence x)
    {
        var dtTz = DateTimeZoneProviders.Tzdb[timeZone];
        // We don't care about timezone of x.Period.StartTime, because we have TimeZoneinfo in @event.At.TimeZone
        // Here we just need to get times from occurrences and convert then to TimeZone times
        var start = x.Period.StartTime.Value.ToLocalDateTime().InZoneLeniently(dtTz).ToDateTimeOffset()
            .ToUniversalTime();
        var end = start.Add(eventPeriod.Duration());

        return new Period<DateTimeOffset>(start, end);
    }
}