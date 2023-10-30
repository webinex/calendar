using Ical.Net.DataTypes;

namespace Webinex.Calendar.Common;

internal static class CalendarExtensions
{
    public static IEnumerable<Occurrence> GetOccurrencesEnumerable(
        this Ical.Net.Calendar calendar,
        DateTime start,
        DateTime? end)
    {
        return GetOccurrencesEnumerable(calendar, new CalDateTime(start),
            end.HasValue ? new CalDateTime(end.Value) : null);
    }

    public static IEnumerable<Occurrence> GetOccurrencesEnumerable(
        this Ical.Net.Calendar calendar,
        CalDateTime start,
        CalDateTime? end)
    {
        return end != null ? calendar.GetOccurrences(start, end) : GetOccurrencesEnumerable(calendar, start);
    }

    private static IEnumerable<Occurrence> GetOccurrencesEnumerable(
        this Ical.Net.Calendar calendar,
        CalDateTime start)
    {
        var occurrences = new HashSet<Occurrence>();

        while (true)
        {
            var result = calendar.GetOccurrences(start, start.AddDays(7));

            foreach (var occurrence in result)
            {
                if (!occurrences.Add(occurrence))
                    continue;

                yield return occurrence;
            }

            start = new CalDateTime(start.AddDays(7));
        }
        // ReSharper disable once IteratorNeverReturns
    }
}