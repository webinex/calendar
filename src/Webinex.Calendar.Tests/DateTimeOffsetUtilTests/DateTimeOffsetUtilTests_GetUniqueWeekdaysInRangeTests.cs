using System;
using FluentAssertions;
using NUnit.Framework;
using Webinex.Calendar.Repeats;

namespace Webinex.Calendar.Tests.DateTimeOffsetUtilTests;

// ReSharper disable once InconsistentNaming
public class DateTimeOffsetUtilTests_GetUniqueWeekdaysInRangeTests
{
    private DateTimeOffset _mondayUtc;
    
    [Test]
    public void WhenPartialInMultipleDays_ShouldBeOk()
    {
        var start = _mondayUtc.AddMinutes(-180);
        var end = _mondayUtc.AddMinutes(-180).AddDays(1).AddMilliseconds(-1);

        var weekdays = DateTimeOffsetUtil.GetUniqueWeekdaysInRange(start, end);
        weekdays.Should().BeEquivalentTo(new[] { Weekday.Sunday, Weekday.Monday });
    }

    [Test]
    public void WhenOneDay_ShouldBeOk()
    {
        var start = _mondayUtc;
        var end = _mondayUtc.AddMinutes(1);
        var weekdays = DateTimeOffsetUtil.GetUniqueWeekdaysInRange(start, end);
        weekdays.Should().BeEquivalentTo(new[] { Weekday.Monday });
    }

    [Test]
    public void WhenSameTime_ShouldBeEmpty()
    {
        var start = _mondayUtc;
        var end = _mondayUtc;
        var weekdays = DateTimeOffsetUtil.GetUniqueWeekdaysInRange(start, end);
        weekdays.Length.Should().Be(0);
    }

    [Test]
    public void WhenFromGtTo_ShouldThrow()
    {
        Assert.Throws<ArgumentException>(() =>
            DateTimeOffsetUtil.GetUniqueWeekdaysInRange(_mondayUtc.AddMilliseconds(1), _mondayUtc));
    }

    [SetUp]
    public void SetUp()
    {
        _mondayUtc = new DateTimeOffset(DateTimeOffset.UtcNow.Date, TimeSpan.Zero);
        _mondayUtc = _mondayUtc.AddDays((int)_mondayUtc.DayOfWeek * -1 + 1);
    }
}