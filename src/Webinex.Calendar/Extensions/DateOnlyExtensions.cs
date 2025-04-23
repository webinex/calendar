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
}