using System;
using System.Linq;
using FluentAssertions;
using NUnit.Framework;
using Webinex.Calendar.Calculators;
using Webinex.Calendar.Extensions;

namespace Webinex.Calendar.Tests;

public class EventEffectivePeriodTests
{
    [TestCase("UTC", "2026-03-01T23:30:00Z", "2026-03-30T00:30:00Z")]
    [TestCase("America/Denver", "2026-03-02T06:30:00Z", "2026-03-30T06:30:00Z")]
    [TestCase("Australia/Sydney", "2026-03-01T12:30:00Z", "2026-03-29T13:30:00Z")]
    [TestCase("UTC", "2026-03-01T00:00:00Z", "2026-03-29T01:00:00Z")]
    [TestCase("America/Denver", "2026-03-01T07:00:00Z", "2026-03-29T07:00:00Z")]
    [TestCase("Australia/Sydney", "2026-02-28T13:00:00Z", "2026-03-28T14:00:00Z")]
    public void WhenSeriesEnds_ShouldUseLastLocalDate(string timeZone, string startValue, string expectedEnd)
    {
        var start = DateTimeOffset.Parse(startValue);
        var @event = Event.Factory.MGDaily(
            Period.New(start, start.AddHours(1)),
            timeZone,
            new TestEventData(),
            until: new DateOnly(2026, 3, 29));
        var query = new OpenPeriod<DateTimeOffset>(start, DateTimeOffset.Parse("2026-04-03T00:00:00Z"));

        var periods = RecurrenceCalculator.Occurrences(@event, query).ToArray();

        periods.Last().End.Should().Be(DateTimeOffset.Parse(expectedEnd));
        @event.Effective().End.Should().Be(DateTimeOffset.Parse(expectedEnd));
    }

    [TestCase(1)]
    [TestCase(7)]
    [TestCase(31)]
    public void WhenSeriesEndsOnOccurrenceDate_ShouldIncludeLastOccurrence(int durationDays)
    {
        var start = DateTimeOffset.Parse("2026-03-01T09:00:00Z");
        var @event = Event.Factory.MGWeekly(
            Period.New(start, start.AddDays(durationDays)),
            "UTC",
            new TestEventData(),
            new[] { DayOfWeek.Sunday },
            until: new DateOnly(2026, 3, 29));

        var effective = @event.Effective();

        effective.End.Should().Be(DateTimeOffset.Parse("2026-03-29T09:00:00Z").AddDays(durationDays));
    }

    [Test]
    public void WhenSeriesContainsOnlyOneLongOccurrence_ShouldIncludeItsFullDuration()
    {
        var start = DateTimeOffset.Parse("2026-03-01T09:00:00Z");
        var @event = Event.Factory.MGWeekly(
            Period.New(start, start.AddDays(7)),
            "UTC",
            new TestEventData(),
            new[] { DayOfWeek.Sunday },
            until: new DateOnly(2026, 3, 1));

        @event.Effective().End.Should().Be(start.AddDays(7));
    }

    [TestCase("2026-03-01T07:00:00Z", 3, 8, "2026-03-15T07:00:00Z")]
    [TestCase("2026-10-25T06:00:00Z", 11, 1, "2026-11-08T06:00:00Z")]
    public void WhenSeriesEndsOnDstTransition_ShouldIncludeLastOccurrence(
        string startValue, int endMonth, int endDay, string expectedEnd)
    {
        var start = DateTimeOffset.Parse(startValue);
        var @event = Event.Factory.MGWeekly(
            Period.New(start, start.AddDays(7)),
            "America/Denver",
            new TestEventData(),
            new[] { DayOfWeek.Sunday },
            until: new DateOnly(2026, endMonth, endDay));

        @event.Effective().End.Should().Be(DateTimeOffset.Parse(expectedEnd));
    }

    [Test]
    public void WhenLastDayHasNoOccurrence_ShouldUseActualLastOccurrenceEnd()
    {
        var start = DateTimeOffset.Parse("2026-03-01T09:00:00Z");
        var @event = Event.Factory.MGWeekly(
            Period.New(start, start.AddDays(7)),
            "UTC",
            new TestEventData(),
            new[] { DayOfWeek.Sunday },
            until: new DateOnly(2026, 3, 31));

        @event.Effective().End.Should().Be(DateTimeOffset.Parse("2026-04-05T09:00:00Z"));
    }
}
