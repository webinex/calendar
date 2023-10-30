using NodaTime;

namespace Webinex.Calendar.Common;

internal static class LocalDateTimeExtensions
{
    public static LocalDateTime ThisOrNext(this LocalDateTime value, DayOfMonth dayOfMonth, TimeSpan time)
    {
        var localTime = LocalTime.FromTimeOnly(TimeOnly.FromTimeSpan(time));
        
        if (value.Day == dayOfMonth.Value && value.TimeOfDay < localTime)
        {
            return value.Date.At(localTime);
        }

        while (value.Day != dayOfMonth.Value)
            value = value.PlusDays(1);

        return value.Date.At(localTime);
    }
}