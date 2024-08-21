using Ical.Net;
using Ical.Net.CalendarComponents;
using Ical.Net.DataTypes;
using Webinex.Calendar.Common;
using Webinex.Calendar.Events;
using Period = Webinex.Calendar.Common.Period;

namespace Webinex.Calendar.Repeats.Calculators;

internal class IntervalRepeatEventCalculator : RepeatEventCalculatorBase
{
    public override IEnumerable<Period> Calculate(RecurrentEvent @event, DateTimeOffset start, DateTimeOffset? end)
    {
        var calendarEvent = GetCalendarEvent(@event);
        return GetOccurrences(calendarEvent, start, end);
    }

    private IEnumerable<Period> GetOccurrences(CalendarEvent calendarEvent, DateTimeOffset start, DateTimeOffset? end)
    {
        var period = new OpenPeriod(start.ToUtc(), end?.ToUtc());

        var calendar = new Ical.Net.Calendar
        {
            TimeZones = { VTimeZone.FromDateTimeZone("UTC") }
        };
        calendar.Events.Add(calendarEvent);

        var occurrences = calendar.GetOccurrencesEnumerable(
            new CalDateTime(start.DateTime.Unspecified(), "UTC"),
            end.HasValue
                ? new CalDateTime(end.Value.DateTime.Unspecified(), "UTC").Subtract(TimeSpan.FromMilliseconds(1))
                : null);

        return occurrences
            .Select(x => new Period(x.Period.StartTime.AsDateTimeOffset, x.Period.EndTime.AsDateTimeOffset))
            .Where(x => period.Intersects(x));
    }

    private CalendarEvent GetCalendarEvent(RecurrentEvent @event)
    {
        var eventStart = Constants.J1_1990.AddMinutes(@event.Repeat.Interval!.StartSince1990Minutes);
        var eventEnd = eventStart.AddMinutes(@event.Repeat.Interval.DurationMinutes);

        return new CalendarEvent
        {
            Start = new CalDateTime(eventStart.Year, eventStart.Month, eventStart.Day, eventStart.Hour, eventStart.Minute, eventStart.Second, "UTC"),
            End = new CalDateTime(eventEnd.Year, eventEnd.Month, eventEnd.Day, eventEnd.Hour, eventEnd.Minute, eventEnd.Second, "UTC"),

            RecurrenceRules =
            {
                // Don't remove Until date, otherwise CalendarExtensions.GetOccurrencesEnumerable won't work correctly,
                // because it checks Until dates of rules
                new RecurrencePattern(FrequencyType.Minutely, interval: @event.Repeat.Interval.IntervalMinutes)
                {
                    // We have to do this, because Until is inclusive
                    Until = @event.Effective.End?.DateTime.AddMilliseconds(-1) ?? DateTime.MaxValue,
                },
            },
        };
    }
}