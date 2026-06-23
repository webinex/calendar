using System;
using System.Linq;
using FluentAssertions;
using NUnit.Framework;
using Webinex.Calendar.Calculators;
using Webinex.Calendar.Extensions;
using Webinex.Calendar.MicrosoftGraph;

namespace Webinex.Calendar.Tests;

public class RecurrenceCalculatorTests
{
    [Test]
    public void Occurrences_WhenRangeEndIsNull_ShouldEnumerateUntilRecurrenceEnd()
    {
        var @event = Event.Factory.MGWeekly(
            Period.New(JAN1_2023_UTC.AddHours(6), JAN1_2023_UTC.AddHours(7)),
            TimeZoneInfo.Utc.Id,
            new None(),
            [DayOfWeek.Sunday],
            until: DateOnly.FromDateTime(JAN1_2023_UTC.AddDays(14).DateTime));

        var result = Occurrences(@event, JAN1_2023_UTC, null);

        result.Should().Equal(
            Period.New(JAN1_2023_UTC.AddHours(6), JAN1_2023_UTC.AddHours(7)),
            Period.New(JAN1_2023_UTC.AddDays(7).AddHours(6), JAN1_2023_UTC.AddDays(7).AddHours(7)),
            Period.New(JAN1_2023_UTC.AddDays(14).AddHours(6), JAN1_2023_UTC.AddDays(14).AddHours(7)));
    }

    [Test]
    public void Occurrences_WhenWeeklyRecurrenceHasUntil_ShouldIncludeUntilDate()
    {
        var @event = Event.Factory.MGWeekly(
            Period.New(JAN1_2023_UTC.AddHours(6), JAN1_2023_UTC.AddHours(7)),
            TimeZoneInfo.Utc.Id,
            new None(),
            [DayOfWeek.Sunday],
            until: DateOnly.FromDateTime(JAN1_2023_UTC.AddDays(7).DateTime));

        var result = Occurrences(@event, JAN1_2023_UTC, JAN1_2023_UTC.AddDays(14));

        result.Should().Equal(
            Period.New(JAN1_2023_UTC.AddHours(6), JAN1_2023_UTC.AddHours(7)),
            Period.New(JAN1_2023_UTC.AddDays(7).AddHours(6), JAN1_2023_UTC.AddDays(7).AddHours(7)));
    }

    [Test]
    public void Occurrences_WhenWeeklyEventStartsBeforeRangeAndOverlapsRangeStart_ShouldMatch()
    {
        var @event = Event.Factory.MGWeekly(
            Period.New(JAN1_2023_UTC.AddHours(6), JAN1_2023_UTC.AddHours(8)),
            TimeZoneInfo.Utc.Id,
            new None(),
            [DayOfWeek.Sunday]);

        var result = Occurrences(@event, JAN1_2023_UTC.AddHours(7), JAN1_2023_UTC.AddHours(9));

        result.Should().Equal(
            Period.New(JAN1_2023_UTC.AddHours(6), JAN1_2023_UTC.AddHours(8)));
    }

    [Test]
    public void Occurrences_WhenOccurrenceStartsAtRangeEnd_ShouldNotMatch()
    {
        var @event = Event.Factory.MGWeekly(
            Period.New(JAN1_2023_UTC.AddHours(6), JAN1_2023_UTC.AddHours(7)),
            TimeZoneInfo.Utc.Id,
            new None(),
            [DayOfWeek.Sunday]);

        var result = Occurrences(@event, JAN1_2023_UTC, JAN1_2023_UTC.AddHours(6));

        result.Should().BeEmpty();
    }

    [Test]
    public void Occurrences_WhenOccurrenceEndsAtRangeStart_ShouldNotMatch()
    {
        var @event = Event.Factory.MGWeekly(
            Period.New(JAN1_2023_UTC.AddHours(6), JAN1_2023_UTC.AddHours(7)),
            TimeZoneInfo.Utc.Id,
            new None(),
            [DayOfWeek.Sunday]);

        var result = Occurrences(@event, JAN1_2023_UTC.AddHours(7), JAN1_2023_UTC.AddHours(8));

        result.Should().BeEmpty();
    }

    [Test]
    public void Occurrences_WhenRelativeMonthlyLastMonday_ShouldReturnLastMondayOfMonth()
    {
        var @event = Recurrent(
            Period.New(JAN1_2023_UTC.AddHours(6), JAN1_2023_UTC.AddHours(7)),
            MGRecurrencePattern.RelativeMonthly([DayOfWeek.Monday], MGRecurrenceRelativeIndex.Last));

        var result = Occurrences(@event, JAN1_2023_UTC, JAN1_2023_UTC.AddMonths(2));

        result.Should().Equal(
            Period.New(DateTimeOffset.Parse("2023-01-30T06:00:00+00:00"), DateTimeOffset.Parse("2023-01-30T07:00:00+00:00")),
            Period.New(DateTimeOffset.Parse("2023-02-27T06:00:00+00:00"), DateTimeOffset.Parse("2023-02-27T07:00:00+00:00")));
    }

    [Test]
    public void Occurrences_WhenAbsoluteYearly_ShouldReturnConfiguredMonthAndDay()
    {
        var @event = Recurrent(
            Period.New(JAN1_2023_UTC.AddHours(6), JAN1_2023_UTC.AddHours(7)),
            MGRecurrencePattern.AbsoluteYearly(dayOfMonth: 10, month: 2));

        var result = Occurrences(@event, JAN1_2023_UTC, JAN1_2023_UTC.AddYears(2));

        result.Should().Equal(
            Period.New(DateTimeOffset.Parse("2023-02-10T06:00:00+00:00"), DateTimeOffset.Parse("2023-02-10T07:00:00+00:00")),
            Period.New(DateTimeOffset.Parse("2024-02-10T06:00:00+00:00"), DateTimeOffset.Parse("2024-02-10T07:00:00+00:00")));
    }

    [Test]
    public void Occurrences_WhenRelativeYearlyFirstMondayOfFebruary_ShouldReturnConfiguredDate()
    {
        var @event = Recurrent(
            Period.New(JAN1_2023_UTC.AddHours(6), JAN1_2023_UTC.AddHours(7)),
            MGRecurrencePattern.RelativeYearly([DayOfWeek.Monday], 2, MGRecurrenceRelativeIndex.First));

        var result = Occurrences(@event, JAN1_2023_UTC, JAN1_2023_UTC.AddYears(2));

        result.Should().Equal(
            Period.New(DateTimeOffset.Parse("2023-02-06T06:00:00+00:00"), DateTimeOffset.Parse("2023-02-06T07:00:00+00:00")),
            Period.New(DateTimeOffset.Parse("2024-02-05T06:00:00+00:00"), DateTimeOffset.Parse("2024-02-05T07:00:00+00:00")));
    }

    [Test]
    public void Occurrences_WhenAbsoluteMonthlyDayDoesNotExistInMonth_ShouldSkipInvalidMonth()
    {
        var @event = Recurrent(
            Period.New(DateTimeOffset.Parse("2023-01-31T06:00:00+00:00"), DateTimeOffset.Parse("2023-01-31T07:00:00+00:00")),
            MGRecurrencePattern.AbsoluteMonthly(dayOfMonth: 31));

        var result = Occurrences(
            @event,
            DateTimeOffset.Parse("2023-02-01T00:00:00+00:00"),
            DateTimeOffset.Parse("2023-04-01T00:00:00+00:00"));

        result.Should().Equal(
            Period.New(DateTimeOffset.Parse("2023-03-31T06:00:00+00:00"), DateTimeOffset.Parse("2023-03-31T07:00:00+00:00")));
    }

    [Test]
    public void Occurrences_WhenEventCrossesMidnight_ShouldPreserveDurationAcrossDays()
    {
        var @event = Recurrent(
            Period.New(DateTimeOffset.Parse("2023-01-01T23:30:00+00:00"), DateTimeOffset.Parse("2023-01-02T01:00:00+00:00")),
            MGRecurrencePattern.Daily());

        var result = Occurrences(
            @event,
            DateTimeOffset.Parse("2023-01-02T00:00:00+00:00"),
            DateTimeOffset.Parse("2023-01-04T00:00:00+00:00"));

        result.Should().Equal(
            Period.New(DateTimeOffset.Parse("2023-01-01T23:30:00+00:00"), DateTimeOffset.Parse("2023-01-02T01:00:00+00:00")),
            Period.New(DateTimeOffset.Parse("2023-01-02T23:30:00+00:00"), DateTimeOffset.Parse("2023-01-03T01:00:00+00:00")),
            Period.New(DateTimeOffset.Parse("2023-01-03T23:30:00+00:00"), DateTimeOffset.Parse("2023-01-04T01:00:00+00:00")));
    }

    [Test]
    public void Occurrences_WhenDstCreatesSkippedLocalTime_ShouldUseLenientMappingConsistently()
    {
        var @event = Recurrent(
            Period.New(DateTimeOffset.Parse("2024-03-30T01:30:00+00:00"), DateTimeOffset.Parse("2024-03-30T02:30:00+00:00")),
            MGRecurrencePattern.Daily(),
            "Europe/London");

        var result = Occurrences(
            @event,
            DateTimeOffset.Parse("2024-03-31T00:00:00+00:00"),
            DateTimeOffset.Parse("2024-04-01T00:00:00+00:00"));

        result.Should().Equal(
            Period.New(DateTimeOffset.Parse("2024-03-31T01:30:00+00:00"), DateTimeOffset.Parse("2024-03-31T02:30:00+00:00")));
    }

    [Test]
    public void Occurrences_WhenDstCreatesAmbiguousLocalTime_ShouldUseExpectedOccurrence()
    {
        var @event = Recurrent(
            Period.New(DateTimeOffset.Parse("2023-10-28T00:30:00+00:00"), DateTimeOffset.Parse("2023-10-28T01:30:00+00:00")),
            MGRecurrencePattern.Daily(),
            "Europe/London");

        var result = Occurrences(
            @event,
            DateTimeOffset.Parse("2023-10-29T00:00:00+00:00"),
            DateTimeOffset.Parse("2023-10-30T00:00:00+00:00"));

        result.Should().Equal(
            Period.New(DateTimeOffset.Parse("2023-10-29T00:30:00+00:00"), DateTimeOffset.Parse("2023-10-29T01:30:00+00:00")));
    }

    [Test]
    public void Occurrences_WhenUntilDateBeforeFirstOccurrence_ShouldReturnEmpty()
    {
        var @event = Recurrent(
            Period.New(DateTimeOffset.Parse("2023-01-02T06:00:00+00:00"), DateTimeOffset.Parse("2023-01-02T07:00:00+00:00")),
            MGRecurrencePattern.Weekly([DayOfWeek.Sunday]),
            recurrenceStart: DateOnly.Parse("2023-01-01"),
            recurrenceEnd: DateOnly.Parse("2023-01-01"));

        var result = Occurrences(
            @event,
            DateTimeOffset.Parse("2023-01-01T00:00:00+00:00"),
            DateTimeOffset.Parse("2023-01-15T00:00:00+00:00"));

        result.Should().BeEmpty();
    }

    [Test]
    public void Occurrences_WhenRecurrencePeriodStartIsAfterEventPeriodStart_ShouldNotEmitBeforeStartDate()
    {
        var @event = Recurrent(
            Period.New(DateTimeOffset.Parse("2023-01-01T06:00:00+00:00"), DateTimeOffset.Parse("2023-01-01T07:00:00+00:00")),
            MGRecurrencePattern.Weekly([DayOfWeek.Sunday]),
            recurrenceStart: DateOnly.Parse("2023-01-08"));

        var result = Occurrences(
            @event,
            DateTimeOffset.Parse("2023-01-01T00:00:00+00:00"),
            DateTimeOffset.Parse("2023-01-15T00:00:00+00:00"));

        result.Should().Equal(
            Period.New(DateTimeOffset.Parse("2023-01-08T06:00:00+00:00"), DateTimeOffset.Parse("2023-01-08T07:00:00+00:00")));
    }

    private static Period<DateTimeOffset>[] Occurrences(
        Event<None> @event,
        DateTimeOffset start,
        DateTimeOffset? end)
    {
        return RecurrenceCalculator.Occurrences(@event, new OpenPeriod<DateTimeOffset>(start, end)).ToArray();
    }

    private static Event<None> Recurrent(
        Period<DateTimeOffset> period,
        MGRecurrencePattern pattern,
        string? timeZone = null,
        DateOnly? recurrenceStart = null,
        DateOnly? recurrenceEnd = null)
    {
        timeZone ??= TimeZoneInfo.Utc.Id;
        var recurrence = new Recurrence(
            new MGRecurrence(
                new MGRecurrencePeriod(recurrenceStart ?? DateOnly.FromDateTime(period.Start.DateTime), recurrenceEnd),
                pattern));

        return Event.New(period, timeZone, new None(), recurrence);
    }
}
