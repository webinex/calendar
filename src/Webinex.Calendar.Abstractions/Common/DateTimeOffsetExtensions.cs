namespace Webinex.Calendar;

public static class DateTimeOffsetExtensions
{
    /// <summary>
    ///     Converts <see cref="DateTimeOffset"/> to <see cref="DateOnly"/> without time zone conversion.
    /// </summary>
    /// <param name="value">The <see cref="DateTimeOffset"/> to be converted</param>
    /// <returns><see cref="DateOnly"/> result</returns>
    public static DateOnly ToDateOnly(this DateTimeOffset value)
    {
        return new DateOnly(value.Year, value.Month, value.Day);
    }
}