using System.Text;
using Ical.Net.CalendarComponents;
using Ical.Net.Serialization;

namespace Webinex.Calendar.Extensions;

public static class OccurrenceExtensions
{
    /// <summary>
    ///     Exports occurrence to iCalendar format (ICS).
    ///     You can configure calendar event and calendar itself using <paramref name="configureEvent"/> and <paramref name="configureCalendar"/> parameters respectively.
    /// </summary>
    public static byte[] ExportToIcs<TData>(
        this Occurrence<TData> occurrence,
        string? productId = null,
        Action<CalendarEvent>? configureEvent = null,
        Action<Ical.Net.Calendar>? configureCalendar = null)
        where TData : class, ICloneable
    {
        var calendarEvent = new CalendarEvent
        {
            DtStart = occurrence.Period.Start.ToCalDateTime(occurrence.TimeZone),
            DtEnd = occurrence.Period.End.ToCalDateTime(occurrence.TimeZone),
        };
        
        configureEvent?.Invoke(calendarEvent);

        var calendar = new Ical.Net.Calendar
        {
            ProductId = productId,
        };
        
        configureCalendar?.Invoke(calendar);

        calendar.Events.Add(calendarEvent);

        var serializer = new CalendarSerializer();
        var serializedCalendar = serializer.SerializeToString(calendar);

        return Encoding.UTF8.GetBytes(serializedCalendar);
    }
}