namespace Webinex.Calendar.Tests.Integration.Common;

public static class DateTimeOffsetExtensions
{
    public static DateOnly ToDateOnly(this DateTimeOffset value) => new(value.Year, value.Month, value.Day);

    public static DateTimeOffset StartOfDay(this DateTimeOffset value) =>
        new(value.Year, value.Month, value.Day, 0, 0, 0, value.Offset);

    public static DateTimeOffset StartOfThisOrNext(this DateTimeOffset value, DayOfWeek dayOfWeek)
    {
        value = value.StartOfDay();

        while (true)
        {
            if (value.DayOfWeek == dayOfWeek)
                return value;

            value = value.AddDays(1);
        }
    }
}