namespace Webinex.Calendar.Common;

internal class DateTimeOffsetUtil
{
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