using System;
using Webinex.Calendar.Repeats;

namespace Webinex.Calendar.Tests;

public static class CommonConstants
{
    public static readonly DateTimeOffset JAN1_2023_UTC = new(2023, 1, 1, 0, 0, 0, TimeSpan.Zero);
    public static readonly Weekday JAN1_2023_WEEKDAY = Weekday.Sunday;
}