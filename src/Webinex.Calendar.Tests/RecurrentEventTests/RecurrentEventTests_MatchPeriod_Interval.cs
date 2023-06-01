using FluentAssertions;
using NUnit.Framework;
using Webinex.Calendar.Common;
using Webinex.Calendar.Events;

namespace Webinex.Calendar.Tests.RecurrentEventTests;

// ReSharper disable once InconsistentNaming
public class RecurrentEventTests_MatchPeriod_Interval
{
    private RecurrentEvent<object> _subject = null!;

    [Test]
    public void WhenBeforeEffectiveStart_ShouldReturnNull()
    {
        _subject.MatchPeriod(_subject.Effective.Start.AddMinutes(-1)).Should().BeNull();
    }

    [Test]
    public void WhenMatchFirst_ShouldReturnStartPeriod()
    {
        var result = _subject.MatchPeriod(_subject.Effective.Start);
        result.Should().BeEquivalentTo(
            new Period(
                JAN1_2023_UTC,
                JAN1_2023_UTC.TotalMinuteOfDay(60)));
    }

    [Test]
    public void WhenAfterEffectiveEnd_ShouldReturnNull()
    {
        _subject.MatchPeriod(_subject.Effective.End!.Value.AddMinutes(-1)).Should().BeNull();
    }

    [Test]
    public void WhenMatchEffectiveEnd_ShouldReturnNull()
    {
        _subject.MatchPeriod(_subject.Effective.End!.Value).Should().BeNull();
    }

    [Test]
    public void WhenMatchLastPeriodEnd_ShouldReturnNull()
    {
        _subject.MatchPeriod(_subject.LastPeriod(_subject.Effective.End!.Value)!.End).Should().BeNull();
    }

    [Test]
    public void WhenMatchLastPeriodStart_ShouldReturnMatch()
    {
        var lastPeriod = _subject.LastPeriod(_subject.Effective.End!.Value)!;
        var result = _subject.MatchPeriod(lastPeriod.Start);
        result.Should().NotBeNull();
        result.Should().BeEquivalentTo(lastPeriod);
    }

    [SetUp]
    public void SetUp()
    {
        _subject = RecurrentEvent<object>.NewInterval(
            JAN1_2023_UTC,
            JAN1_2023_UTC.AddMonths(1),
            7 * 24 * 60, // every week
            60,
            new object());
    }
}