using System;
using FluentAssertions;
using NUnit.Framework;
using Webinex.Calendar.Calculators;
using Webinex.Calendar.MicrosoftGraph;

namespace Webinex.Calendar.Tests;

public class RecurrenceCalculatorTests_FirstOccurrence
{
    private const string UTC = "UTC";
    private const string NEW_YORK = "America/New_York";

    // Tuesday
    private static readonly DateTimeOffset START = DateTimeOffset.Parse("2026-11-03T10:00:00Z");
    private static readonly Period<DateTimeOffset> PERIOD = Period.New(START, START.AddHours(1));

    [Test]
    public void WhenOneTime_ShouldReturnPeriodAsIs()
    {
        RecurrenceCalculator.FirstOccurrence(PERIOD, UTC, null).Should().Be(PERIOD);
    }

    [Test]
    public void WhenStartMatchesRecurrence_ShouldReturnPeriodAsIs()
    {
        var recurrence = Weekly(START, null, DayOfWeek.Tuesday);

        RecurrenceCalculator.FirstOccurrence(PERIOD, UTC, recurrence).Should().Be(PERIOD);
    }

    [Test]
    public void WhenStartDoesntMatchWeekdays_ShouldReturnFirstMatchingDay()
    {
        var recurrence = Weekly(START, null, DayOfWeek.Monday, DayOfWeek.Wednesday);

        RecurrenceCalculator.FirstOccurrence(PERIOD, UTC, recurrence)
            .Should().Be(Period.New(START.AddDays(1), START.AddDays(1).AddHours(1)));
    }

    [Test]
    public void WhenStartDoesntMatchDayOfMonth_ShouldReturnFirstMatchingDay()
    {
        var recurrence = new Recurrence(new MGRecurrence(
            new MGRecurrencePeriod(DateOnly.FromDateTime(START.UtcDateTime), null),
            MGRecurrencePattern.AbsoluteMonthly(15, 1)));

        RecurrenceCalculator.FirstOccurrence(PERIOD, UTC, recurrence)
            .Should().Be(Period.New(START.AddDays(12), START.AddDays(12).AddHours(1)));
    }

    [Test]
    public void WhenFirstMatchingDayAfterDstChange_ShouldKeepLocalTime()
    {
        // Saturday 10:00 EST, first Monday is after spring forward, so it's 10:00 EDT
        var start = DateTimeOffset.Parse("2026-03-07T15:00:00Z");
        var recurrence = Weekly(start.ToOffset(TimeSpan.FromHours(-5)), null, DayOfWeek.Monday);

        var expectedStart = DateTimeOffset.Parse("2026-03-09T14:00:00Z");
        RecurrenceCalculator.FirstOccurrence(Period.New(start, start.AddHours(1)), NEW_YORK, recurrence)
            .Should().Be(Period.New(expectedStart, expectedStart.AddHours(1)));
    }

    [Test]
    public void WhenRecurrenceEndsBeforeFirstMatchingDay_ShouldReturnNull()
    {
        var recurrence = Weekly(START, DateOnly.FromDateTime(START.UtcDateTime).AddDays(2), DayOfWeek.Monday);

        RecurrenceCalculator.FirstOccurrence(PERIOD, UTC, recurrence).Should().BeNull();
    }

    private static Recurrence Weekly(DateTimeOffset start, DateOnly? until, params DayOfWeek[] daysOfWeek)
    {
        return new Recurrence(new MGRecurrence(
            new MGRecurrencePeriod(DateOnly.FromDateTime(start.DateTime), until),
            MGRecurrencePattern.Weekly(daysOfWeek, 1, null)));
    }
}
