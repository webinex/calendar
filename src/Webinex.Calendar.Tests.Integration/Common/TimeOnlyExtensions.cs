namespace Webinex.Calendar.Tests.Integration.Common;

public static class TimeOnlyExtensions
{
    public static int TotalMinutes(this TimeOnly time) => (int)time.ToTimeSpan().TotalMinutes;
}