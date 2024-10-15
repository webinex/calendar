namespace Webinex.Calendar.Tests.Integration.Common;

public static class DateTimeOffsetExtensions
{
    public static DateTimeOffset WithTime(this DateTimeOffset value, TimeOnly time) => new DateTimeOffset(
        value.Year, value.Month, value.Day, time.Hour, time.Minute, time.Second,
        time.Millisecond, time.Microsecond, value.Offset);
}