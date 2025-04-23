using Ical.Net;
using Ical.Net.DataTypes;
using Webinex.Calendar.MicrosoftGraph;

namespace Webinex.Calendar.Calculators;

internal static class MGRecurrencePatternConverter
{
    public static RecurrencePattern ConvertToIcal(MGRecurrencePattern pattern, DateTime until)
    {
        var result = new RecurrencePattern
        {
            Interval = pattern.Interval,
            Frequency = MapFrequencyType(pattern.Type),
            
            // Don't remove Until date, otherwise CalendarExtensions.GetOccurrencesEnumerable won't work correctly,
            // because it checks Until dates of rules
            Until = until,
        };

        if (pattern.DaysOfWeek is { Count: > 0 })
        {
            result.ByDay = pattern.DaysOfWeek
                .Select(x => new WeekDay(x))
                .ToList();
        }

        if (pattern.Index != null)
        {
            result.BySetPosition = new List<int> { MapIndex(pattern.Index.Value) };
        }

        if (pattern.DayOfMonth.HasValue)
        {
            result.ByMonthDay = new List<int> { pattern.DayOfMonth.Value };
        }

        if (pattern.Month.HasValue)
        {
            result.ByMonth = new List<int> { pattern.Month.Value };
        }

        if (pattern.FirstDayOfWeek.HasValue)
        {
            result.FirstDayOfWeek = pattern.FirstDayOfWeek.Value;
        }

        return result;
    }

    private static int MapIndex(MGRecurrenceRelativeIndex index)
    {
        return index switch
        {
            MGRecurrenceRelativeIndex.First => 1,
            MGRecurrenceRelativeIndex.Second => 2,
            MGRecurrenceRelativeIndex.Third => 3,
            MGRecurrenceRelativeIndex.Fourth => 4,
            MGRecurrenceRelativeIndex.Last => -1,
            _ => throw new ArgumentException($"Invalid index: {index}")
        };
    }

    private static FrequencyType MapFrequencyType(RecurrenceType type)
    {
        return type switch
        {
            RecurrenceType.Daily => FrequencyType.Daily,
            RecurrenceType.Weekly => FrequencyType.Weekly,
            RecurrenceType.AbsoluteMonthly => FrequencyType.Monthly,
            RecurrenceType.RelativeMonthly => FrequencyType.Monthly,
            RecurrenceType.AbsoluteYearly => FrequencyType.Yearly,
            RecurrenceType.RelativeYearly => FrequencyType.Yearly,
            _ => throw new ArgumentException($"Unsupported recurrence type: {type}")
        };
    }
}