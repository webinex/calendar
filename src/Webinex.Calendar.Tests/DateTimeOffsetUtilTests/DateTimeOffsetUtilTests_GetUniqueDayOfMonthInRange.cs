using System;
using FluentAssertions;
using NUnit.Framework;

namespace Webinex.Calendar.Tests.DateTimeOffsetUtilTests;

// ReSharper disable once InconsistentNaming
public class DateTimeOffsetUtilTests_GetUniqueDayOfMonthInRange
{
    [Test]
    public void WhenFromGtTo_ShouldThrow()
    {
        Assert.Throws<ArgumentException>(() =>
            DateTimeOffsetUtil.GetUniqueDayOfMonthInRange(JAN1_2023_UTC.AddMilliseconds(1), JAN1_2023_UTC));
    }

    [Test]
    public void WhenEqual_ShouldBeEmpty()
    {
        var result = DateTimeOffsetUtil.GetUniqueDayOfMonthInRange(JAN1_2023_UTC, JAN1_2023_UTC);
        result.Length.Should().Be(0);
    }

    [Test]
    public void WhenSameDay_ShouldBeOne()
    {
        var result = DateTimeOffsetUtil.GetUniqueDayOfMonthInRange(JAN1_2023_UTC, JAN1_2023_UTC.AddMilliseconds(1));
        result.Should().BeEquivalentTo(new[] { 1 });
    }

    [Test]
    public void WhenYear_ShouldBe31()
    {
        var result = DateTimeOffsetUtil.GetUniqueDayOfMonthInRange(JAN1_2023_UTC, JAN1_2023_UTC.AddYears(1));
        result.Length.Should().Be(31);
    }

    [Test]
    public void WhenOverNight_ShouldBe2()
    {
        var result =
            DateTimeOffsetUtil.GetUniqueDayOfMonthInRange(JAN1_2023_UTC.AddHours(23), JAN1_2023_UTC.AddHours(25));

        result.Should().BeEquivalentTo(new[] { 1, 2 });
    }

    [Test]
    public void WhenOverMonth_ShouldBeOk()
    {
        var result = DateTimeOffsetUtil.GetUniqueDayOfMonthInRange(
            JAN1_2023_UTC.AddMilliseconds(-1), JAN1_2023_UTC.AddMilliseconds(1));

        result.Should().BeEquivalentTo(new[] { 1, 31 });
    }

    [Test]
    public void WhenEndAtZero_ShouldBeExcluded()
    {
        var result = DateTimeOffsetUtil.GetUniqueDayOfMonthInRange(
            JAN1_2023_UTC.AddMilliseconds(-1), JAN1_2023_UTC);

        result.Should().BeEquivalentTo(new[] { 31 });
    }
}