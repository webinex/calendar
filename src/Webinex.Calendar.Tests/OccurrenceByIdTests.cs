using System;
using System.Linq;
using FluentAssertions;
using NUnit.Framework;
using Webinex.Calendar.Calculators;
using Webinex.Calendar.Extensions;

namespace Webinex.Calendar.Tests;

public class OccurrenceByIdTests
{
    private const string DENVER = "America/Denver";

    // 2026-03-08 is spring forward day in America/Denver, 02:00 - 03:00 local time doesn't exist
    [TestCase("2026-03-05T09:30:00Z", 15)]
    [TestCase("2026-03-05T09:30:00Z", 30)]
    [TestCase("2026-03-05T09:30:00Z", 59)]
    [TestCase("2026-03-05T09:00:00Z", 15)]
    public void WhenOccurrenceFallsIntoDstGap_ShouldCalculateById(string startValue, int durationMinutes)
    {
        var start = DateTimeOffset.Parse(startValue);
        var @event = Event.Factory.MGDaily(
            Period.New(start, start.AddMinutes(durationMinutes)),
            DENVER,
            new TestEventData());

        var occurrences = new OccurrenceCalculator<TestEventData>(
                Period.New(start, start.AddDays(5)),
                new IEventEntityBase[] { @event })
            .Calculate();

        occurrences.Should().Contain(x => x.Period.Start == DateTimeOffset.Parse("2026-03-08T09:30:00Z") ||
                                          x.Period.Start == DateTimeOffset.Parse("2026-03-08T09:00:00Z"));
        foreach (var occurrence in occurrences)
        {
            var result = OccurrenceCalculator<TestEventData>.TryCalculate(
                OccurrenceId.Parse(occurrence.Id), @event, null);

            result.Should().NotBeNull();
            result!.Period.Should().Be(occurrence.Period);
        }
    }

    [Test]
    public void WhenOccurrenceFallsIntoDstGap_ShouldMoveItForward()
    {
        var start = DateTimeOffset.Parse("2026-03-05T09:30:00Z");
        var @event = Event.Factory.MGDaily(Period.New(start, start.AddMinutes(30)), DENVER, new TestEventData());

        var periods = RecurrenceCalculator.Occurrences(
                @event,
                new OpenPeriod<DateTimeOffset>(
                    DateTimeOffset.Parse("2026-03-08T09:30:00Z"),
                    DateTimeOffset.Parse("2026-03-08T09:31:00Z")))
            .ToArray();

        periods.Should().BeEquivalentTo(new[]
        {
            Period.New(DateTimeOffset.Parse("2026-03-08T09:30:00Z"), DateTimeOffset.Parse("2026-03-08T10:00:00Z")),
        });
    }

    [Test]
    public void WhenOccurrenceCancelled_ShouldReturnNull()
    {
        var start = DateTimeOffset.Parse("2026-10-26T16:00:00Z");
        var @event = Event.Factory.MGDaily(Period.New(start, start.AddHours(1)), DENVER, new TestEventData());
        var id = OccurrenceId.Recurrent(@event.Id, start.AddDays(1));
        var adjustment = OccurrenceAdjustment<TestEventData>.NewCancel(
            @event.Id,
            @event.Group,
            Period.New(id.GetStartOrThrow(), id.GetStartOrThrow().AddHours(1)));

        OccurrenceCalculator<TestEventData>.TryCalculate(id, @event, adjustment).Should().BeNull();
    }

    // Search period is built from id start (original slot), MoveTo is applied after occurrence found
    [TestCase(3, null)]
    [TestCase(-3, null)]
    [TestCase(3, 1)]
    public void WhenOccurrenceMovedWithoutData_ShouldReturnMovedOccurrence(int moveDays, int? untilAfterDays)
    {
        var start = DateTimeOffset.Parse("2026-10-26T16:00:00Z");
        var @event = Event.Factory.MGDaily(
            Period.New(start, start.AddHours(1)),
            DENVER,
            new TestEventData(),
            until: untilAfterDays.HasValue ? new DateOnly(2026, 10, 26 + untilAfterDays.Value) : null);
        var id = OccurrenceId.Recurrent(@event.Id, start.AddDays(1));
        var originalPeriod = Period.New(id.GetStartOrThrow(), id.GetStartOrThrow().AddHours(1));
        var moveTo = Period.New(originalPeriod.Start.AddDays(moveDays), originalPeriod.End.AddDays(moveDays));
        var adjustment = OccurrenceAdjustment<TestEventData>.NewMove(@event.Id, @event.Group, originalPeriod, moveTo);

        var result = OccurrenceCalculator<TestEventData>.TryCalculate(id, @event, adjustment);

        result.Should().NotBeNull();
        result!.Id.Should().Be(id.ToString());
        result.Period.Should().Be(moveTo);
    }

    [Test]
    public void WhenOccurrenceIsAfterSeriesEnd_ShouldReturnNull()
    {
        var start = DateTimeOffset.Parse("2026-10-26T16:00:00Z");
        var @event = Event.Factory.MGDaily(
            Period.New(start, start.AddHours(1)),
            DENVER,
            new TestEventData(),
            until: new DateOnly(2026, 10, 27));
        var id = OccurrenceId.Recurrent(@event.Id, start.AddDays(2));

        OccurrenceCalculator<TestEventData>.TryCalculate(id, @event, null).Should().BeNull();
    }

    [TestCase(true)]
    [TestCase(false)]
    public void WhenNotMovedAdjustmentIsAfterSeriesEnd_ShouldReturnNull(bool withData)
    {
        var start = DateTimeOffset.Parse("2026-10-26T16:00:00Z");
        var @event = Event.Factory.MGDaily(
            Period.New(start, start.AddHours(1)),
            DENVER,
            new TestEventData(),
            until: new DateOnly(2026, 10, 27));
        var id = OccurrenceId.Recurrent(@event.Id, start.AddDays(2));
        var adjustment = OccurrenceAdjustment<TestEventData>.NewUpdate(
            id,
            Period.New(id.GetStartOrThrow(), id.GetStartOrThrow().AddHours(1)),
            @event.Group,
            withData ? new TestEventData() : null,
            moveTo: null);

        OccurrenceCalculator<TestEventData>.TryCalculate(id, @event, adjustment).Should().BeNull();
    }

    [Test]
    public void WhenNotMovedAdjustmentOccurrenceNotGenerated_ShouldNotReturnIt()
    {
        var start = DateTimeOffset.Parse("2026-10-26T16:00:00Z");
        var @event = Event.Factory.MGDaily(
            Period.New(start, start.AddHours(1)),
            DENVER,
            new TestEventData(),
            until: new DateOnly(2026, 10, 27));
        var adjustmentStart = start.AddDays(3);
        var adjustment = OccurrenceAdjustment<TestEventData>.NewData(
            @event.Id,
            @event.Group,
            Period.New(adjustmentStart, adjustmentStart.AddHours(1)),
            new TestEventData());

        var occurrences = new OccurrenceCalculator<TestEventData>(
                Period.New(adjustmentStart, adjustmentStart.AddHours(1)),
                new IEventEntityBase[] { @event, adjustment })
            .Calculate();

        occurrences.Should().BeEmpty();
    }

    [Test]
    public void WhenOccurrenceIdDoesntMatchRecurrence_ShouldReturnNull()
    {
        var start = DateTimeOffset.Parse("2026-10-26T16:00:00Z");
        var @event = Event.Factory.MGDaily(Period.New(start, start.AddHours(1)), DENVER, new TestEventData());
        var id = OccurrenceId.Recurrent(@event.Id, start.AddDays(1).AddMinutes(30));

        OccurrenceCalculator<TestEventData>.TryCalculate(id, @event, null).Should().BeNull();
    }
}
