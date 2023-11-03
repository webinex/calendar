using Ical.Net;
using Ical.Net.CalendarComponents;
using Ical.Net.DataTypes;
using NodaTime;
using NodaTime.Extensions;
using Webinex.Calendar.Common;
using Webinex.Calendar.Events;
using Period = Webinex.Calendar.Common.Period;

namespace Webinex.Calendar.Repeats.Calculators;

internal class WeekdayRepeatEventCalculator : RepeatEventCalculatorBase
{
    public override IEnumerable<Period> Calculate(RecurrentEvent @event, DateTimeOffset start, DateTimeOffset? end)
    {
        var calendarEvent = GetCalendarEvent(@event);
        return GetOccurrences(@event, calendarEvent, start, end);
    }

    private CalendarEvent GetCalendarEvent(RecurrentEvent @event)
    {
        var (effectiveStartTz, effectiveEndTz) = Tz(@event.Effective, @event.Repeat.Weekday!.TimeZone);

        var eventStart =
            effectiveStartTz.Date.At(LocalTime.FromMinutesSinceMidnight(@event.Repeat.Weekday.TimeOfTheDayInMinutes));
        var eventEnd = eventStart.PlusMinutes(@event.Repeat.Weekday.DurationMinutes);
        var weekdays = @event.Repeat.Weekday.Weekdays.Select(x => new WeekDay(x.ToDayOfWeek()));

        return new CalendarEvent
        {
            Start = new CalDateTime(eventStart.ToDateTimeUnspecified(), "UTC"),
            End = new CalDateTime(eventEnd.ToDateTimeUnspecified(), "UTC"),
            RecurrenceRules =
            {
                new RecurrencePattern(FrequencyType.Weekly, @event.Repeat.Weekday.Interval ?? 1)
                {
                    Until = effectiveEndTz?.ToDateTimeUnspecified() ?? DateTime.MaxValue,
                    ByDay = weekdays.ToList(),
                },
            },
        };
    }

    private IEnumerable<Period> GetOccurrences(
        RecurrentEvent @event,
        CalendarEvent calendarEvent,
        DateTimeOffset start,
        DateTimeOffset? end)
    {
        var period = new OpenPeriod(start.ToUtc(), end?.ToUtc());
        var tz = DateTimeZoneProviders.Tzdb[@event.Repeat.Weekday!.TimeZone];
        var calendar = new Ical.Net.Calendar();
        calendar.Events.Add(calendarEvent);

        var occurrences = calendar.GetOccurrencesEnumerable(
            start.ToInstant().InZone(tz).ToDateTimeUnspecified(),
            end?.ToInstant().InZone(tz).ToDateTimeUnspecified().AddMilliseconds(-1));

        return occurrences.Select(x => Map(@event, x)).Where(x => period.Intersects(x));
    }

    private Period Map(RecurrentEvent @event, Occurrence x)
    {
        var dtTz = DateTimeZoneProviders.Tzdb[@event.Repeat.Weekday!.TimeZone];
        var startTz = x.Period.StartTime.Value.Unspecified().ToLocalDateTime().InZoneLeniently(dtTz);

        return new Period(
            startTz.ToDateTimeOffset().ToUtc(),
            startTz.ToDateTimeOffset().ToUtc().AddMinutes(@event.Repeat.Weekday.DurationMinutes));
    }

    private (LocalDateTime start, LocalDateTime? end) Tz(OpenPeriod period, string tz)
    {
        var start = Tz(period.Start, tz);
        var end = period.End.HasValue ? Tz(period.End.Value, tz) : default(LocalDateTime?);
        return (start, end);
    }

    private LocalDateTime Tz(DateTimeOffset value, string tz)
    {
        var localDateTime = value.ToUtc().DateTime.ToLocalDateTime();
        return localDateTime.InZoneLeniently(DateTimeZoneProviders.Tzdb[tz]).LocalDateTime;
    }
}