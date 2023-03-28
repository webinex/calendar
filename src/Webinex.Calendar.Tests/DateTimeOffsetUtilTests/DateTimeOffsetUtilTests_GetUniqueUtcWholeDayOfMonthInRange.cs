using System;
using System.Linq;
using FluentAssertions;
using NUnit.Framework;
using Webinex.Calendar.Common;

namespace Webinex.Calendar.Tests.DateTimeOffsetUtilTests;

// ReSharper disable once InconsistentNaming
public class DateTimeOffsetUtilTests_GetUniqueUtcWholeDayOfMonthInRange
{
    [Test]
    public void WhenFromGtTo_ShouldThrow()
    {
        Assert.Throws<ArgumentException>(() =>
            DateTimeOffsetUtil.GetUniqueUtcWholeDayOfMonthInRange(JAN1_2023_UTC.AddMilliseconds(1), JAN1_2023_UTC));
    }

    [Test]
    public void WhenEqual_ShouldBeEmpty()
    {
        var result = DateTimeOffsetUtil.GetUniqueUtcWholeDayOfMonthInRange(JAN1_2023_UTC, JAN1_2023_UTC);
        result.Length.Should().Be(0);
    }

    [Test]
    public void WhenSameDay_ShouldBeEmpty()
    {
        var result = DateTimeOffsetUtil.GetUniqueUtcWholeDayOfMonthInRange(JAN1_2023_UTC, JAN1_2023_UTC.AddDays(1).AddMilliseconds(-1));
        result.Should().BeEmpty();
    }

    [Test]
    public void WhenNextDay_ShouldBeOne()
    {
        var result = DateTimeOffsetUtil.GetUniqueUtcWholeDayOfMonthInRange(JAN1_2023_UTC, JAN1_2023_UTC.AddDays(1));
        result.Should().BeEquivalentTo(new[] { 1 });
    }

    [Test]
    public void WhenYear_ShouldBeAll()
    {
        var result = DateTimeOffsetUtil.GetUniqueUtcWholeDayOfMonthInRange(JAN1_2023_UTC, JAN1_2023_UTC.AddYears(1));
        result.Should().BeEquivalentTo(Enumerable.Range(1, 31));
    }

    [Test]
    public void WhenOverNight_ShouldBe1()
    {
        var result =
            DateTimeOffsetUtil.GetUniqueUtcWholeDayOfMonthInRange(JAN1_2023_UTC, JAN1_2023_UTC.AddHours(25));

        result.Should().BeEquivalentTo(new[] { 1 });
    }

    [Test]
    public void WhenOverMonth_ShouldBeOk()
    {
        var result = DateTimeOffsetUtil.GetUniqueUtcWholeDayOfMonthInRange(
            JAN1_2023_UTC.AddDays(-1), JAN1_2023_UTC.AddDays(1));

        result.Should().BeEquivalentTo(new[] { 1, 31 });
    }

    [Test]
    public void WhenDifferentDaysButPeriodLessThan24Hours_ShouldBeEmpty()
    {
        var result = DateTimeOffsetUtil.GetUniqueUtcWholeDayOfMonthInRange(
            JAN1_2023_UTC.AddHours(-1), JAN1_2023_UTC.AddHours(23).AddMilliseconds(-1));

        result.Should().BeEmpty();
    }
}