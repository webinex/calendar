namespace Webinex.Calendar;

public static class OpenPeriodExtensions
{
    public static OpenPeriod<DateTimeOffset> ToUtc(this OpenPeriod<DateTimeOffset> value)
    {
        return new OpenPeriod<DateTimeOffset>(value.Start.ToUniversalTime(), value.End?.ToUniversalTime());
    }

    public static bool Intersects<T>(this OpenPeriod<DateTimeOffset> x, Period<DateTimeOffset> y)
    {
        return x.Start.CompareTo(y.End) < 0 && (x.End == null || x.End.Value.CompareTo(y.Start) > 0);
    }
}