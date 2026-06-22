using Ical.Net.DataTypes;
using NodaTime;
using NodaTime.Extensions;

namespace Webinex.Calendar.Extensions;

public static class DateTimeOffsetExtensions
{
    public static DateTimeOffset StartOfMinute(this DateTimeOffset value)
    {
        return new DateTimeOffset(value.Year, value.Month, value.Day, value.Hour, value.Minute, 0, 0, value.Offset);
    }

    public static DateOnly ToDateOnly(this DateTimeOffset value, string timeZone)
    {
        return value.ToLocalDateTime(timeZone).Date.ToDateOnly();
    }

    public static LocalDateTime ToLocalDateTime(this DateTimeOffset value, string timeZone)
    {
        var tz = DateTimeZoneProviders.Tzdb[timeZone];
        return value.ToInstant().InZone(tz).LocalDateTime;
    }

    public static CalDateTime ToCalDateTime(this DateTimeOffset value, string timeZone)
    {
        var localDateTime = value.ToLocalDateTime(timeZone);
        return new CalDateTime(localDateTime.Year, localDateTime.Month, localDateTime.Day, localDateTime.Hour, localDateTime.Minute, localDateTime.Second, timeZone);
    }
}