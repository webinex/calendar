using Ical.Net;
using Ical.Net.CalendarComponents;
using Ical.Net.DataTypes;
using NodaTime;
using NodaTime.Extensions;
using Webinex.Calendar.Common;
using Webinex.Calendar.Events;
using Period = Webinex.Calendar.Common.Period;

namespace Webinex.Calendar.Repeats.Calculators;

internal class DayOfMonthRepeatEventCalculator : RepeatEventCalculatorBase
{
    public override IEnumerable<Period> Calculate(RecurrentEvent @event, DateTimeOffset start, DateTimeOffset? end)
    {
        var calendarEvent = GetCalendarEvent(@event);
        return GetOccurrences(@event, calendarEvent, start, end);
    }

    private CalendarEvent GetCalendarEvent(RecurrentEvent @event)
    {
        var tz = DateTimeZoneProviders.Tzdb[@event.Repeat.DayOfMonth!.TimeZone.Id];
        var effectiveStart = @event.Effective.Start.DateTime.ToLocalDateTime().InZoneLeniently(tz);
        var effectiveEnd = @event.Effective.End?.DateTime.ToLocalDateTime().InZoneLeniently(tz);
        var eventStart = effectiveStart.LocalDateTime.ThisOrNext(@event.Repeat.DayOfMonth.DayOfMonth,
            TimeSpan.FromMinutes(@event.Repeat.DayOfMonth.TimeOfTheDayInMinutes));
        var eventEnd = eventStart.PlusMinutes(@event.Repeat.DayOfMonth.DurationMinutes);

        var calendarEvent = new CalendarEvent
        {
            Start = new CalDateTime(eventStart.ToDateTimeUnspecified()),
            End = new CalDateTime(eventEnd.ToDateTimeUnspecified()),
        };

        var recurrencePattern = new RecurrencePattern(FrequencyType.Monthly)
        {
            ByMonthDay = new List<int>
            {
                @event.Repeat.DayOfMonth.DayOfMonth.Value,
            },

            Until = effectiveEnd?.ToDateTimeUnspecified() ?? DateTime.MaxValue,
        };

        calendarEvent.RecurrenceRules.Add(recurrencePattern);
        return calendarEvent;
    }

    private IEnumerable<Period> GetOccurrences(
        RecurrentEvent @event,
        CalendarEvent calendarEvent,
        DateTimeOffset start,
        DateTimeOffset? end)
    {
        var period = new OpenPeriod(start.ToUtc(), end?.ToUtc());
        var calendar = new Ical.Net.Calendar();
        calendar.Events.Add(calendarEvent);

        var startTz = TimeZoneInfo.ConvertTimeFromUtc(start.ToUtc().DateTime, @event.Repeat.DayOfMonth!.TimeZone);
        var endTz = end.HasValue
            ? TimeZoneInfo.ConvertTimeFromUtc(end.Value.ToUtc().DateTime, @event.Repeat.DayOfMonth.TimeZone)
            : default(DateTime?);

        var occurrences = calendar.GetOccurrencesEnumerable(startTz, endTz?.AddMilliseconds(-1));
        return occurrences.Select(x => Map(@event, x)).Where(x => period.Intersects(x));
    }

    private Period Map(RecurrentEvent @event, Occurrence occurrence)
    {
        var tz = DateTimeZoneProviders.Tzdb[@event.Repeat.DayOfMonth!.TimeZone.Id];
        var eventStartTz = occurrence.Period.StartTime.Value.ToLocalDateTime().InZoneLeniently(tz);

        return new Period(
            eventStartTz.ToDateTimeOffset().ToUtc(),
            eventStartTz.ToDateTimeOffset().ToUtc().AddMinutes(@event.Repeat.DayOfMonth!.DurationMinutes));
    }
}