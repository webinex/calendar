using NodaTime;
using NodaTime.Extensions;

namespace Webinex.Calendar.Extensions;

public static class PeriodExtensions
{
    public static OpenPeriod<LocalDateTime> InZone(this OpenPeriod<DateTimeOffset> period, string timeZone)
    {
        var (start, end) = InZone(period.Start, period.End, timeZone);
        return new OpenPeriod<LocalDateTime>(start, end);
    }

    public static Period<LocalDateTime> InZone(this Period<DateTimeOffset> period, string timeZone)
    {
        var (start, end) = InZone(period.Start, period.End, timeZone);
        return new Period<LocalDateTime>(start, end!.Value);
    }

    private static (LocalDateTime start, LocalDateTime? end) InZone(DateTimeOffset start, DateTimeOffset? end,
        string timeZone)
    {
        var tz = DateTimeZoneProviders.Tzdb[timeZone];
        var startTz = start.ToInstant().InZone(tz).LocalDateTime;
        var endTz = end?.ToInstant().InZone(tz).LocalDateTime;
        return (startTz, endTz);
    }

    public static Period<DateTimeOffset> InZone(this Period<DateOnly> period, string timeZone)
    {
        var tz = DateTimeZoneProviders.Tzdb[timeZone];
        var start = period.Start.ToLocalDate().At(LocalTime.Midnight).InZoneLeniently(tz).ToDateTimeOffset();
        var end = period.End.ToLocalDate().At(LocalTime.Midnight).InZoneLeniently(tz).ToDateTimeOffset();
        return new Period<DateTimeOffset>(start, end);
    }
    
    public static Period<DateTimeOffset> ToUtc(this Period<DateTimeOffset> value)
    {
        return new Period<DateTimeOffset>(value.Start.ToUniversalTime(), value.End.ToUniversalTime());
    }
    
    public static OpenPeriod<DateTimeOffset> ToOpenPeriod(this Period<DateTimeOffset> value)
    {
        return new OpenPeriod<DateTimeOffset>(value.Start, value.End);
    }

    public static TimeSpan Duration(this Period<DateTimeOffset> period)
    {
        return period.End - period.Start;
    }
}