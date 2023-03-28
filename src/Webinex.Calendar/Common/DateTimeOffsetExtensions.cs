namespace Webinex.Calendar.Common;

internal static class DateTimeOffsetExtensions
{
    public static DateTimeOffset ToUtc(this DateTimeOffset value)
    {
        return value.ToOffset(TimeSpan.Zero);
    }

    public static DateTimeOffset StartOfTheDayUtc(this DateTimeOffset value)
    {
        return new DateTimeOffset(value.ToUtc().Date, TimeSpan.Zero);
    }

    public static int TotalMinutesFromStartOfTheDayUtc(this DateTimeOffset value)
    {
        var start = value.StartOfTheDayUtc();
        return (int)(value - start).TotalMinutes;
    }

    public static long TotalMinutesSince1990(this DateTimeOffset value)
    {
        value = value.ToUtc();

        if (value.Second > 0 || value.Millisecond > 0)
            throw new InvalidOperationException("Might not contain seconds and milliseconds");

        if (value < Constants.J1_1990)
            throw new InvalidOperationException("Might be greater than or equal to J1 1990");

        return (long)(value - Constants.J1_1990).TotalMinutes;
    }
}