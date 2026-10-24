using System;
using System.Linq;
using FluentAssertions;
using NUnit.Framework;
using Webinex.Calendar.Calculators;
using Webinex.Calendar.Extensions;

namespace Webinex.Calendar.Tests;

public class OccurrenceOverlapTests
{
    private static Period<DateTimeOffset> Query => Period.New(
        DateTimeOffset.Parse("2026-03-31T00:00:00Z"),
        DateTimeOffset.Parse("2026-04-01T00:00:00Z"));

    private static Period<DateTimeOffset> OverlappingPeriod => Period.New(
        DateTimeOffset.Parse("2026-03-29T09:00:00Z"),
        DateTimeOffset.Parse("2026-04-05T09:00:00Z"));

    [Test]
    public void WhenSevenDayOccurrenceStartsBeforeQuery_ShouldReturnOverlappingOccurrence()
    {
        var @event = CreateWeeklyEvent();

        var periods = RecurrenceCalculator.Occurrences(@event, Query.ToOpenPeriod()).ToArray();

        periods.Should().BeEquivalentTo(new[] { OverlappingPeriod });
    }

    [Test]
    public void WhenOverlappingOccurrenceHasUpdatedData_ShouldReturnUpdatedData()
    {
        var @event = CreateWeeklyEvent();
        var adjustment = OccurrenceAdjustment<EventData>.NewData(
            @event.Id,
            @event.Group,
            OverlappingPeriod,
            new EventData { Title = "updated" });

        var occurrences = new OccurrenceCalculator<EventData>(
            Query,
            new IEventEntityBase[] { @event, adjustment }).Calculate();

        occurrences.Should().ContainSingle().Which.Data.Title.Should().Be("updated");
    }

    [Test]
    public void WhenOverlappingOccurrenceIsCancelled_ShouldNotReturnOccurrence()
    {
        var @event = CreateWeeklyEvent();
        var adjustment = OccurrenceAdjustment<EventData>.NewCancel(
            @event.Id,
            @event.Group,
            OverlappingPeriod);

        var occurrences = new OccurrenceCalculator<EventData>(
            Query,
            new IEventEntityBase[] { @event, adjustment }).Calculate();

        occurrences.Should().BeEmpty();
    }

    [Test]
    public void WhenMultipleLongOccurrencesOverlap_ShouldReturnAllOverlappingOccurrences()
    {
        var @event = CreateWeeklyEvent(durationDays: 31);

        var periods = RecurrenceCalculator.Occurrences(@event, Query.ToOpenPeriod()).ToArray();

        periods.Should().BeEquivalentTo(new[] { 0, 7, 14, 21, 28 }.Select(days =>
            Period.New(@event.Period.Start.AddDays(days), @event.Period.End.AddDays(days))));
    }

    [Test]
    public void WhenEarlierOccurrencesOverlapIdLookup_ShouldReturnRequestedOccurrence()
    {
        var @event = CreateWeeklyEvent(durationDays: 31);
        var id = new OccurrenceId(@event.Id, OverlappingPeriod.Start);

        var occurrence = OccurrenceCalculator<EventData>.CalculateRecurrent(id, @event, null);

        occurrence.Id.Should().Be(id.ToString());
        occurrence.Period.Start.Should().Be(id.Start);
    }

    [Test]
    public void WhenEarlierOccurrencesOverlapMovedIdLookup_ShouldReturnRequestedMovedOccurrence()
    {
        var @event = CreateWeeklyEvent(durationDays: 31);
        var originalPeriod = Period.New(OverlappingPeriod.Start, OverlappingPeriod.Start.AddDays(31));
        var movedPeriod = Period.New(originalPeriod.Start.AddDays(1), originalPeriod.End.AddDays(1));
        var id = new OccurrenceId(@event.Id, originalPeriod.Start);
        var adjustment = OccurrenceAdjustment<EventData>.NewMove(
            @event.Id,
            @event.Group,
            originalPeriod,
            movedPeriod);

        var occurrence = OccurrenceCalculator<EventData>.CalculateRecurrent(id, @event, adjustment);

        occurrence.Id.Should().Be(id.ToString());
        occurrence.Period.Should().Be(movedPeriod);
    }

    [Test]
    public void WhenOccurrenceEndsAtQueryStart_ShouldNotReturnEndedOccurrence()
    {
        var @event = CreateWeeklyEvent();
        var range = new OpenPeriod<DateTimeOffset>(
            OverlappingPeriod.Start,
            OverlappingPeriod.Start.AddHours(1));

        var periods = RecurrenceCalculator.Occurrences(@event, range).ToArray();

        periods.Should().ContainSingle().Which.Should().Be(OverlappingPeriod);
    }

    [Test]
    public void WhenOccurrenceStartsAtQueryEnd_ShouldNotReturnOccurrence()
    {
        var @event = CreateWeeklyEvent(durationDays: 1);
        var range = new OpenPeriod<DateTimeOffset>(
            OverlappingPeriod.Start.AddDays(-1),
            OverlappingPeriod.Start);

        RecurrenceCalculator.Occurrences(@event, range).Should().BeEmpty();
    }

    [Test]
    public void WhenQueryPrecedesSeries_ShouldNotReturnOccurrences()
    {
        var @event = CreateWeeklyEvent();
        var range = new OpenPeriod<DateTimeOffset>(
            DateTimeOffset.Parse("2026-02-01T00:00:00Z"),
            DateTimeOffset.Parse("2026-02-02T00:00:00Z"));

        RecurrenceCalculator.Occurrences(@event, range).Should().BeEmpty();
    }

    [Test]
    public void WhenOccurrencesOverlapAcrossDst_ShouldPreserveFixedDuration()
    {
        var @event = CreateWeeklyEvent(
            timeZone: "America/Denver",
            start: DateTimeOffset.Parse("2026-03-01T07:00:00Z"));
        var range = new OpenPeriod<DateTimeOffset>(
            DateTimeOffset.Parse("2026-03-15T06:30:00Z"),
            DateTimeOffset.Parse("2026-03-15T06:45:00Z"));

        var periods = RecurrenceCalculator.Occurrences(@event, range).ToArray();

        periods.Should().BeEquivalentTo(new[]
        {
            Period.New(
                DateTimeOffset.Parse("2026-03-08T07:00:00Z"),
                DateTimeOffset.Parse("2026-03-15T07:00:00Z")),
            Period.New(
                DateTimeOffset.Parse("2026-03-15T06:00:00Z"),
                DateTimeOffset.Parse("2026-03-22T06:00:00Z")),
        });
    }

    private static Event<EventData> CreateWeeklyEvent(
        int durationDays = 7,
        string timeZone = "UTC",
        DateTimeOffset? start = null)
    {
        var origin = start ?? DateTimeOffset.Parse("2026-03-01T09:00:00Z");
        return Event.Factory.MGWeekly(
            Period.New(origin, origin.AddDays(durationDays)),
            timeZone,
            new EventData(),
            new[] { DayOfWeek.Sunday });
    }

    private sealed class EventData : ICloneable
    {
        public string Title { get; init; } = "original";

        public object Clone() => new EventData { Title = Title };
    }
}
