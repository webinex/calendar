namespace Webinex.Calendar;

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
}