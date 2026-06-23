namespace Webinex.Calendar;

public static class CalendarConstants
{
    public static readonly DateTime MAX_DATE_TIME = new(3000, 1, 1);
    public static readonly DateTimeOffset MAX_DATE_TIME_OFFSET = new(3000, 1, 1, 0, 0, 0, TimeSpan.Zero);
    public static readonly DateOnly MAX_DATE_ONLY = new(3000, 1, 1);
}