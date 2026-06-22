using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using NUnit.Framework;
using Webinex.Calendar.Calculators;
using Webinex.Calendar.Extensions;

namespace Webinex.Calendar.Tests;

public class OccurrenceCalculatorTests
{
    [Test]
    public void Calculate_WhenContainsOneTimeEvents_ShouldReturnOrderedOccurrences()
    {
        var later = OneTime(JAN1_2023_UTC.AddHours(3), JAN1_2023_UTC.AddHours(4));
        var earlier = OneTime(JAN1_2023_UTC.AddHours(1), JAN1_2023_UTC.AddHours(2));

        var result = Calculate(JAN1_2023_UTC, JAN1_2023_UTC.AddHours(5), later, earlier);

        result.Select(x => x.Period).Should().Equal(earlier.Period, later.Period);
    }

    [Test]
    public void Calculate_WhenRecurrentOccurrenceHasMoveAdjustment_ShouldReturnMovedPeriod()
    {
        var @event = Weekly([DayOfWeek.Sunday]);
        var original = Period.New(JAN1_2023_UTC.AddHours(6), JAN1_2023_UTC.AddHours(7));
        var moved = Period.New(JAN1_2023_UTC.AddHours(8), JAN1_2023_UTC.AddHours(9));
        var adjustment = OccurrenceAdjustment<None>.NewMove(@event.Id, @event.Group, original, moved);

        var result = Calculate(JAN1_2023_UTC, JAN1_2023_UTC.AddDays(1), @event, adjustment);

        result.Should().ContainSingle()
            .Which.Period.Should().BeEquivalentTo(moved);
    }

    [Test]
    public void Calculate_WhenRecurrentOccurrenceHasDataAdjustment_ShouldReturnAdjustedData()
    {
        var @event = Weekly([DayOfWeek.Sunday]);
        var original = Period.New(JAN1_2023_UTC.AddHours(6), JAN1_2023_UTC.AddHours(7));
        var data = new None();
        var adjustment = OccurrenceAdjustment<None>.NewData(@event.Id, @event.Group, original, data);

        var result = Calculate(JAN1_2023_UTC, JAN1_2023_UTC.AddDays(1), @event, adjustment);

        result.Should().ContainSingle()
            .Which.Data.Should().NotBeSameAs(@event.Data);
    }

    [Test]
    public void Calculate_WhenRecurrentOccurrenceCancelled_ShouldSkipOccurrence()
    {
        var @event = Weekly([DayOfWeek.Sunday]);
        var original = Period.New(JAN1_2023_UTC.AddHours(6), JAN1_2023_UTC.AddHours(7));
        var adjustment = OccurrenceAdjustment<None>.NewCancel(@event.Id, @event.Group, original);

        var result = Calculate(JAN1_2023_UTC, JAN1_2023_UTC.AddDays(1), @event, adjustment);

        result.Should().BeEmpty();
    }

    [Test]
    public void Calculate_WhenAdjustmentMovedIntoPeriodButOriginalIsOutside_ShouldReturnMovedOccurrence()
    {
        var @event = Weekly([DayOfWeek.Sunday]);
        var original = Period.New(JAN1_2023_UTC.AddHours(6), JAN1_2023_UTC.AddHours(7));
        var moved = Period.New(JAN1_2023_UTC.AddDays(1).AddHours(6), JAN1_2023_UTC.AddDays(1).AddHours(7));
        var adjustment = OccurrenceAdjustment<None>.NewMove(@event.Id, @event.Group, original, moved);

        var result = Calculate(JAN1_2023_UTC.AddDays(1), JAN1_2023_UTC.AddDays(2), @event, adjustment);

        result.Should().ContainSingle()
            .Which.Period.Should().BeEquivalentTo(moved);
    }

    [Test]
    public void CalculateEnumerable_WhenRecurrentEventIsEndless_ShouldAllowTakingLimitedOccurrences()
    {
        var @event = Weekly([DayOfWeek.Sunday]);

        var result = new OccurrenceCalculator<None>(
                new OpenPeriod<DateTimeOffset>(JAN1_2023_UTC, null),
                [@event])
            .CalculateEnumerable()
            .Take(2)
            .ToArray();

        result.Select(x => x.Period).Should().Equal(
            Period.New(JAN1_2023_UTC.AddHours(6), JAN1_2023_UTC.AddHours(7)),
            Period.New(JAN1_2023_UTC.AddDays(7).AddHours(6), JAN1_2023_UTC.AddDays(7).AddHours(7)));
    }

    [Test]
    public void CalculateEnumerable_WhenEventsAreUnordered_ShouldReturnOrderedOccurrences()
    {
        var later = OneTime(JAN1_2023_UTC.AddHours(3), JAN1_2023_UTC.AddHours(4));
        var earlier = OneTime(JAN1_2023_UTC.AddHours(1), JAN1_2023_UTC.AddHours(2));

        var result = new OccurrenceCalculator<None>(
                Period.New(JAN1_2023_UTC, JAN1_2023_UTC.AddHours(5)),
                [later, earlier])
            .CalculateEnumerable()
            .ToArray();

        result.Select(x => x.Period).Should().Equal(earlier.Period, later.Period);
    }

    [Test]
    public void CalculateEnumerable_WhenRecurrentEventsAreUnordered_ShouldReturnOrderedOccurrences()
    {
        var later = Weekly([DayOfWeek.Monday]);
        var earlier = Weekly([DayOfWeek.Sunday]);

        var result = new OccurrenceCalculator<None>(
                Period.New(JAN1_2023_UTC, JAN1_2023_UTC.AddDays(3)),
                [later, earlier])
            .CalculateEnumerable()
            .ToArray();

        result.Select(x => x.Period).Should().Equal(
            Period.New(JAN1_2023_UTC.AddHours(6), JAN1_2023_UTC.AddHours(7)),
            Period.New(JAN1_2023_UTC.AddDays(1).AddHours(6), JAN1_2023_UTC.AddDays(1).AddHours(7)));
    }

    [Test]
    public void CalculateEnumerable_WhenMovedOccurrenceComesBeforeGeneratedOccurrence_ShouldReturnMovedOccurrenceFirst()
    {
        var @event = Weekly([DayOfWeek.Sunday]);
        var original = Period.New(JAN1_2023_UTC.AddDays(7).AddHours(6), JAN1_2023_UTC.AddDays(7).AddHours(7));
        var moved = Period.New(JAN1_2023_UTC.AddHours(5), JAN1_2023_UTC.AddHours(6));
        var adjustment = OccurrenceAdjustment<None>.NewMove(@event.Id, @event.Group, original, moved);

        var result = new OccurrenceCalculator<None>(
                Period.New(JAN1_2023_UTC, JAN1_2023_UTC.AddDays(8)),
                [@event, adjustment])
            .CalculateEnumerable()
            .Take(1)
            .ToArray();

        result.Should().ContainSingle()
            .Which.Period.Should().BeEquivalentTo(moved);
    }

    [Test]
    public void TryCalculateOccurrenceAdjustment_WhenDataIsNull_ShouldReturnFalse()
    {
        var @event = Weekly([DayOfWeek.Sunday]);
        var original = Period.New(JAN1_2023_UTC.AddHours(6), JAN1_2023_UTC.AddHours(7));
        var moved = Period.New(JAN1_2023_UTC.AddHours(8), JAN1_2023_UTC.AddHours(9));
        var adjustment = OccurrenceAdjustment<None>.NewMove(@event.Id, @event.Group, original, moved);

        var result = OccurrenceCalculator<None>.TryCalculateOccurrenceAdjustment(adjustment, out var occurrence);

        result.Should().BeFalse();
        occurrence.Should().BeNull();
    }

    private static IReadOnlyCollection<Occurrence<None>> Calculate(
        DateTimeOffset start,
        DateTimeOffset end,
        params IEventEntityBase[] entries)
    {
        return new OccurrenceCalculator<None>(Period.New(start, end), entries).Calculate();
    }

    private static Event<None> OneTime(DateTimeOffset start, DateTimeOffset end)
    {
        return Event.New(Period.New(start, end), TimeZoneInfo.Utc.Id, new None());
    }

    private static Event<None> Weekly(DayOfWeek[] daysOfWeek, DateOnly? until = null)
    {
        return Event.Factory.MGWeekly(
            Period.New(JAN1_2023_UTC.AddHours(6), JAN1_2023_UTC.AddHours(7)),
            TimeZoneInfo.Utc.Id,
            new None(),
            daysOfWeek,
            until: until);
    }
}
