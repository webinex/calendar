using System;

namespace Webinex.Calendar.Tests;

public static class DateTimeOffsetExtensions
{
    public static DateTimeOffset Add(this DateTimeOffset value, string time)
    {
        return value.Add(TimeSpan.Parse(time));
    }
}