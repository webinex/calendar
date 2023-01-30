using Webinex.Calendar.Repeats;

namespace Webinex.Calendar;

internal class DateTimeOffsetUtil
{
    public static Weekday[] GetUniqueWeekdaysInRange(DateTimeOffset from, DateTimeOffset to)
    {
        if (from > to)
            throw new ArgumentException("Might be greater than or equal to `to`", nameof(from));

        if (from == to)
            return Array.Empty<Weekday>();
        
        if ((to - from).TotalDays > 7)
            return Weekday.All;

        var referenceValue = from;
        var weekdays = new List<Weekday>();

        while (referenceValue < to)
        {
            weekdays.Add(Weekday.From(referenceValue.DayOfWeek));
            referenceValue = referenceValue.AddDays(1);
        }

        weekdays.Add(Weekday.From(to.DayOfWeek));
        return weekdays.Distinct().ToArray();
    }

    public static int[] GetUniqueDayOfMonthInRange(DateTimeOffset from, DateTimeOffset to)
    {
        if (from > to)
            throw new ArgumentException("Cannot be greater than `to`", nameof(from));

        if (from == to)
            return Array.Empty<int>();
        
        var daysOfMonth = new List<int>();
        var referenceValue = from;

        while (referenceValue < to)
        {
            daysOfMonth.Add(referenceValue.Day);
            referenceValue = referenceValue.AddDays(1);
        }
        
        if (to.TimeOfDay > TimeSpan.Zero)
            daysOfMonth.Add(to.Day);

        return daysOfMonth.OrderBy(x => x).Distinct().ToArray();
    }

    public static Weekday[] GetUniqueUtcWholeWeekdaysInRange(DateTimeOffset from, DateTimeOffset to)
    {
        if (from > to)
            throw new ArgumentException($"Cannot be less than {nameof(from)}", nameof(to));
        
        from = from.ToOffset(TimeSpan.Zero);
        to = to.ToOffset(TimeSpan.Zero);

        var value = from.TimeOfDay > TimeSpan.Zero
            ? from.AddDays(1).StartOfTheDayUtc()
            : from;
        
        var end = to.StartOfTheDayUtc();

        var days = new LinkedList<Weekday>();
        while (value < end)
        {
            days.AddLast(Weekday.From(value.DayOfWeek));
            value = value.AddDays(1);
        }

        return days.Distinct().ToArray();
    }

    public static int[] GetUniqueUtcWholeDayOfMonthInRange(DateTimeOffset from, DateTimeOffset to)
    {
        if (from > to)
            throw new ArgumentException($"Cannot be less than {nameof(from)}", nameof(to));
        
        from = from.ToOffset(TimeSpan.Zero);
        to = to.ToOffset(TimeSpan.Zero);

        var value = from.TimeOfDay > TimeSpan.Zero
            ? from.AddDays(1).StartOfTheDayUtc()
            : from;

        var end = to.StartOfTheDayUtc();

        var days = new LinkedList<int>();
        while (value < end)
        {
            days.AddLast(value.Day);
            value = value.AddDays(1);
        }

        return days.Distinct().ToArray();
    }
}