namespace Webinex.Calendar.Tests.Integration.Common;

public static class DateTimeOffsetExtensions
{
    public static DateOnly ToDateOnly(this DateTimeOffset value) => new(value.Year, value.Month, value.Day);
}