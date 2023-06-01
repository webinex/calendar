using FluentAssertions;
using NUnit.Framework;
using Webinex.Calendar.Common;
using Webinex.Calendar.Events;

namespace Webinex.Calendar.Tests.RecurrentEventTests;

// ReSharper disable once InconsistentNaming
public class RecurrentEventTests_LastPeriod_Interval
{
    private RecurrentEvent<object> _subject = null!;

    [Test]
    public void WhenBeforeEffectiveStart_ShouldReturnNull()
    {
        _subject.LastPeriod(_subject.Effective.Start.AddMinutes(-1)).Should().BeNull();
    }

    [Test]
    public void WhenAfterEffectiveEnd_ShouldReturnLastEffective()
    {
        var result = _subject.LastPeriod(_subject.Effective.End!.Value.AddYears(1));
        result.Should().BeEquivalentTo(
            new Period(
                JAN1_2023_UTC.Day(29).TotalMinuteOfDay(600),
                JAN1_2023_UTC.Day(29).TotalMinuteOfDay(600 + 60)));
    }

    [Test]
    public void WhenMatchBeforeEffectiveEnd_ShouldReturnMatch()
    {
        var result = _subject.LastPeriod(_subject.Effective.Start.Day(17));
        result.Should().BeEquivalentTo(
            new Period(
                JAN1_2023_UTC.Day(15).TotalMinuteOfDay(600),
                JAN1_2023_UTC.Day(15).TotalMinuteOfDay(600 + 60)));
    }

    [SetUp]
    public void SetUp()
    {
        _subject = RecurrentEvent<object>.NewInterval(
            JAN1_2023_UTC.AddMinutes(600),
            JAN1_2023_UTC.AddMonths(1),
            7 * 24 * 60, // every week
            60,
            new object());
    }
}