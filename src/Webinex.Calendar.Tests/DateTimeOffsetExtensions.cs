using System;
using FluentAssertions.Common;

namespace Webinex.Calendar.Tests;

public static class DateTimeOffsetExtensions
{
    public static DateTimeOffset Add(this DateTimeOffset value, string time)
    {
        return value.Add(TimeSpan.Parse(time));
    }

    public static DateTimeOffset Day(this DateTimeOffset value, int day)
    {
        return new DateTimeOffset(value.Year, value.Month, day, value.Hour, value.Minute, value.Second,
            value.Millisecond, value.Offset);
    }

    public static DateTimeOffset TotalMinuteOfDay(this DateTimeOffset value, int totalMinute)
    {
        return value.Date.ToDateTimeOffset().AddMinutes(totalMinute);
    }
}