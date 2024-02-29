namespace Webinex.Calendar.Common;

internal static class DateTimeExtensions
{
    public static DateTime Unspecified(this DateTime value)
    {
        return DateTime.SpecifyKind(value, DateTimeKind.Unspecified);
    }
}