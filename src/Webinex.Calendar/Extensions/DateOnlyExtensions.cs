using NodaTime;

namespace Webinex.Calendar.Extensions;

internal static class DateOnlyExtensions
{
    internal static LocalDateTime ToLocalDateTime(this DateOnly value, int hour = 0, int minute = 0, int second = 0, int millisecond = 0)
    {
        return new LocalDateTime(value.Year, value.Month, value.Day, hour, minute, second, millisecond);
    }
}
