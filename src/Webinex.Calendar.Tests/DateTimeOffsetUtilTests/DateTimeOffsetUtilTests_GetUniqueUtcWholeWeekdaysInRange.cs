using System;
using FluentAssertions;
using NUnit.Framework;
using Webinex.Calendar.Common;
using Webinex.Calendar.Repeats;

namespace Webinex.Calendar.Tests.DateTimeOffsetUtilTests;

// ReSharper disable once InconsistentNaming
public class DateTimeOffsetUtilTests_GetUniqueUtcWholeWeekdaysInRange
{
    [Test]
    public void WhenFromGtTo_ShouldThrow()
    {
        Assert.Throws<ArgumentException>(() =>
            DateTimeOffsetUtil.GetUniqueUtcWholeWeekdaysInRange(JAN1_2023_UTC.AddMilliseconds(1), JAN1_2023_UTC));
    }

    [Test]
    public void WhenEqual_ShouldBeEmpty()
    {
        var result = DateTimeOffsetUtil.GetUniqueUtcWholeWeekdaysInRange(JAN1_2023_UTC, JAN1_2023_UTC);
        result.Length.Should().Be(0);
    }

    [Test]
    public void WhenSameDay_ShouldBeEmpty()
    {
        var result = DateTimeOffsetUtil.GetUniqueUtcWholeWeekdaysInRange(JAN1_2023_UTC, JAN1_2023_UTC.AddDays(1).AddMilliseconds(-1));
        result.Should().BeEmpty();
    }

    [Test]
    public void WhenNextDay_ShouldBeOne()
    {
        var result = DateTimeOffsetUtil.GetUniqueUtcWholeWeekdaysInRange(JAN1_2023_UTC, JAN1_2023_UTC.AddDays(1));
        result.Should().BeEquivalentTo(new[] { Weekday.Sunday });
    }

    [Test]
    public void WhenWeek_ShouldBeAll()
    {
        var result = DateTimeOffsetUtil.GetUniqueUtcWholeWeekdaysInRange(JAN1_2023_UTC, JAN1_2023_UTC.AddDays(7));
        result.Should().BeEquivalentTo(Weekday.All);
    }

    [Test]
    public void WhenOverNight_ShouldBe1()
    {
        var result =
            DateTimeOffsetUtil.GetUniqueUtcWholeWeekdaysInRange(JAN1_2023_UTC, JAN1_2023_UTC.AddHours(25));

        result.Should().BeEquivalentTo(new[] { Weekday.Sunday });
    }

    [Test]
    public void WhenOverMonth_ShouldBeOk()
    {
        var result = DateTimeOffsetUtil.GetUniqueUtcWholeWeekdaysInRange(
            JAN1_2023_UTC.AddDays(-1), JAN1_2023_UTC.AddDays(1));

        result.Should().BeEquivalentTo(new[] { Weekday.Saturday, Weekday.Sunday });
    }

    [Test]
    public void WhenDifferentDaysButPeriodLessThan24Hours_ShouldBeEmpty()
    {
        var result = DateTimeOffsetUtil.GetUniqueUtcWholeWeekdaysInRange(
            JAN1_2023_UTC.AddHours(-1), JAN1_2023_UTC.AddHours(23).AddMilliseconds(-1));

        result.Should().BeEmpty();
    }
}