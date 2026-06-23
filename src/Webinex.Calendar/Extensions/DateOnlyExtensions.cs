using NodaTime;
using NodaTime.Extensions;

namespace Webinex.Calendar.Extensions;

internal static class DateOnlyExtensions
{
    public static LocalDateTime InZone(this DateOnly value, string tzId)
    {
        var tz = DateTimeZoneProviders.Tzdb[tzId];
        return value.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc).ToInstant().InZone(tz).LocalDateTime;
    }
    
    internal static LocalDateTime ToLocalDateTime(this DateOnly value, int hour = 0, int minute = 0, int second = 0, int millisecond = 0)
    {
        return new LocalDateTime(value.Year, value.Month, value.Day, hour, minute, second, millisecond);
    }
}
