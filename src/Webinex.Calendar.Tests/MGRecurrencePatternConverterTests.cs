using System;
using System.Linq;
using FluentAssertions;
using Ical.Net;
using Ical.Net.DataTypes;
using NUnit.Framework;
using Webinex.Calendar.Calculators;
using Webinex.Calendar.MicrosoftGraph;

namespace Webinex.Calendar.Tests;

public class MGRecurrencePatternConverterTests
{
    [Test]
    public void ConvertToIcal_WhenPatternHasFields_ShouldMapThem()
    {
        var pattern = MGRecurrencePattern.RelativeYearly(
            [DayOfWeek.Monday, DayOfWeek.Wednesday],
            month: 2,
            MGRecurrenceRelativeIndex.Last,
            interval: 3,
            firstDayOfWeek: DayOfWeek.Sunday);

        var result = MGRecurrencePatternConverter.ConvertToIcal(pattern, DateTime.MaxValue);

        result.Frequency.Should().Be(FrequencyType.Yearly);
        result.Interval.Should().Be(3);
        result.ByDay.Select(x => x.DayOfWeek).Should().Equal(DayOfWeek.Monday, DayOfWeek.Wednesday);
        result.ByMonth.Should().Equal(2);
        result.BySetPosition.Should().Equal(-1);
        result.FirstDayOfWeek.Should().Be(DayOfWeek.Sunday);
    }

    [Test]
    public void ConvertToIcal_WhenAbsoluteMonthly_ShouldMapMonthDay()
    {
        var result = MGRecurrencePatternConverter.ConvertToIcal(
            MGRecurrencePattern.AbsoluteMonthly(dayOfMonth: 25, interval: 2),
            DateTime.MaxValue);

        result.Frequency.Should().Be(FrequencyType.Monthly);
        result.Interval.Should().Be(2);
        result.ByMonthDay.Should().Equal(25);
    }
}
