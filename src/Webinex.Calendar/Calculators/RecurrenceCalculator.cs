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
        OpenPeriod<DateTimeOffset> @in)
    {
        var eventPeriodZoned = @event.Period.InZone(@event.TimeZone);
        var inZoned = @in.InZone(@event.TimeZone);
        var endZoned = @event.Recurrence!.MGRecurrence!.Period.End?.InZone(@event.TimeZone).ToDateTimeUnspecified().AddDays(1) ??
                       DateTime.MaxValue;

        var pattern = MGRecurrencePatternConverter.ConvertToIcal(@event.Recurrence!.MGRecurrence!.Pattern, endZoned);

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
            inZoned.Start.ToDateTimeUnspecified(),
            // We use .AddMilliseconds(-1) to avoid match events by inclusive end
            // For Ical period of 18:00 - 19:00 will match with event which starts at 19:00
            inZoned.End?.ToDateTimeUnspecified().AddMilliseconds(-1));

        return occurrences.Select(x => Map(@event, x)).Where(x => @in.Intersects(x));
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