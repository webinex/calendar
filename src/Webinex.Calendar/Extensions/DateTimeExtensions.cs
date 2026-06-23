using Ical.Net.DataTypes;

namespace Webinex.Calendar.Extensions;

internal static class DateTimeExtensions
{
    public static CalDateTime AsUTCCalDateTime(this DateTime dateTime)
    {
        return new CalDateTime(dateTime, TimeZoneInfo.Utc.Id);
    }
}