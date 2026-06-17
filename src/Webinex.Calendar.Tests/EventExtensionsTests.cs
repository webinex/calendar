using System;
using FluentAssertions;
using NUnit.Framework;
using Webinex.Calendar.Extensions;

namespace Webinex.Calendar.Tests;

public class EventExtensionsTests
{
    [Test]
    public void Effective_WhenOneTimeEvent_ShouldReturnEventPeriod()
    {
        var period = Period.New(JAN1_2023_UTC.AddHours(6), JAN1_2023_UTC.AddHours(7));
        var @event = Event.New(period, TimeZoneInfo.Utc.Id, new None());

        var result = @event.Effective();

        result.Should().BeEquivalentTo(new OpenPeriod<DateTimeOffset>(period.Start, period.End));
    }

    [Test]
    public void Effective_WhenEndlessRecurrentEvent_ShouldReturnOpenEndedPeriod()
    {
        var @event = Event.Factory.MGWeekly(
            Period.New(JAN1_2023_UTC.AddHours(6), JAN1_2023_UTC.AddHours(7)),
            TimeZoneInfo.Utc.Id,
            new None(),
            [DayOfWeek.Sunday]);

        var result = @event.Effective();

        result.Start.Should().Be(@event.Period.Start);
        result.End.Should().BeNull();
    }

    [Test]
    public void Effective_WhenRecurrentEventHasEndDate_ShouldReturnLastOccurrenceEnd()
    {
        var @event = Event.Factory.MGWeekly(
            Period.New(JAN1_2023_UTC.AddHours(6), JAN1_2023_UTC.AddHours(7)),
            TimeZoneInfo.Utc.Id,
            new None(),
            [DayOfWeek.Sunday],
            until: DateOnly.FromDateTime(JAN1_2023_UTC.AddDays(7).DateTime));

        var result = @event.Effective();

        result.Should().BeEquivalentTo(
            new OpenPeriod<DateTimeOffset>(
                JAN1_2023_UTC.AddHours(6),
                JAN1_2023_UTC.AddDays(7).AddHours(7)));
    }

    [Test]
    public void Effective_WhenRecurrencePeriodStartIsAfterEventPeriodStart_ShouldUseFirstOccurrenceStart()
    {
        var @event = Event.Factory.MGWeekly(
            Period.New(JAN1_2023_UTC.AddHours(6), JAN1_2023_UTC.AddHours(7)),
            TimeZoneInfo.Utc.Id,
            new None(),
            [DayOfWeek.Sunday],
            until: DateOnly.FromDateTime(JAN1_2023_UTC.AddDays(14).DateTime));

        @event.SetRecurrence(@event.Recurrence!.WithStart(DateOnly.FromDateTime(JAN1_2023_UTC.AddDays(7).DateTime)));

        var result = @event.Effective();

        result.Should().BeEquivalentTo(
            new OpenPeriod<DateTimeOffset>(
                JAN1_2023_UTC.AddDays(7).AddHours(6),
                JAN1_2023_UTC.AddDays(14).AddHours(7)));
    }

    [Test]
    public void Effective_WhenRecurrentEventDoesNotHaveAnyOccurrences_ShouldThrow()
    {
        var start = new DateTimeOffset(2026, 6, 18, 6, 0, 0, TimeSpan.Zero);
        var @event = Event.Factory.MGWeekly(
            Period.New(start, start.AddHours(1)),
            TimeZoneInfo.Utc.Id,
            new None(),
            [DayOfWeek.Monday, DayOfWeek.Tuesday],
            until: new DateOnly(2026, 6, 21));

        var act = () => @event.Effective();

        act.Should()
            .Throw<InvalidOperationException>()
            .WithMessage($"Recurrent event {@event.Id} does not have any occurrences");
    }

    [Test]
    public void ValidateAtLeastOneOccurrence_WhenRecurrentEventHasOccurrences_ShouldNotThrow()
    {
        var @event = Event.Factory.MGWeekly(
            Period.New(JAN1_2023_UTC.AddHours(6), JAN1_2023_UTC.AddHours(7)),
            TimeZoneInfo.Utc.Id,
            new None(),
            [DayOfWeek.Sunday],
            until: DateOnly.FromDateTime(JAN1_2023_UTC.AddDays(7).DateTime));

        var act = () => @event.ValidateAtLeastOneOccurrenceOrThrow();

        act.Should().NotThrow();
    }

    [Test]
    public void ValidateAtLeastOneOccurrence_WhenOneTimeEvent_ShouldNotThrow()
    {
        var @event = Event.New(
            Period.New(JAN1_2023_UTC.AddHours(6), JAN1_2023_UTC.AddHours(7)),
            TimeZoneInfo.Utc.Id,
            new None());

        var act = () => @event.ValidateAtLeastOneOccurrenceOrThrow();

        act.Should().NotThrow();
    }

    [Test]
    public void ValidateAtLeastOneOccurrence_WhenRecurrentEventDoesNotHaveAnyOccurrences_ShouldThrow()
    {
        var start = new DateTimeOffset(2026, 6, 18, 6, 0, 0, TimeSpan.Zero);
        var @event = Event.Factory.MGWeekly(
            Period.New(start, start.AddHours(1)),
            TimeZoneInfo.Utc.Id,
            new None(),
            [DayOfWeek.Monday, DayOfWeek.Tuesday],
            until: new DateOnly(2026, 6, 21));

        var act = () => @event.ValidateAtLeastOneOccurrenceOrThrow();

        act.Should()
            .Throw<InvalidOperationException>()
            .WithMessage($"Recurrent event {@event.Id} does not have any occurrences");
    }

    [Test]
    public void Effective_WhenCalledTwice_ShouldReturnSameValue()
    {
        var @event = Event.Factory.MGWeekly(
            Period.New(JAN1_2023_UTC.AddHours(6), JAN1_2023_UTC.AddHours(7)),
            TimeZoneInfo.Utc.Id,
            new None(),
            [DayOfWeek.Sunday],
            until: DateOnly.FromDateTime(JAN1_2023_UTC.AddDays(7).DateTime));

        var first = @event.Effective();
        var second = @event.Effective();

        second.Should().BeEquivalentTo(first);
    }

    [Test]
    public void Effective_WhenRecurrentEventCrossesMidnight_ShouldUseLastOccurrenceEnd()
    {
        var @event = Event.Factory.MGWeekly(
            Period.New(JAN1_2023_UTC.AddHours(23).AddMinutes(30), JAN1_2023_UTC.AddDays(1).AddHours(1)),
            TimeZoneInfo.Utc.Id,
            new None(),
            [DayOfWeek.Sunday],
            until: DateOnly.FromDateTime(JAN1_2023_UTC.AddDays(7).DateTime));

        var result = @event.Effective();

        result.Should().BeEquivalentTo(
            new OpenPeriod<DateTimeOffset>(
                JAN1_2023_UTC.AddHours(23).AddMinutes(30),
                JAN1_2023_UTC.AddDays(8).AddHours(1)));
    }

}
