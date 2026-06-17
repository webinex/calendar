using System;
using NUnit.Framework;
using Webinex.Calendar.Common;

namespace Webinex.Calendar.Tests.RepeatEventCalculatorTests;

// ReSharper disable once InconsistentNaming
public class RepeatEventCalculatorTests_DayOfMonth
{
    [Test]
    public void WhenMatchOne_ShouldBeOk()
    {
        new RepeatEventCalculatorScenario()
            .WithRange(JAN1_2023_UTC.Add("6:00"), JAN1_2023_UTC.Add("6:01"))
            .WithAbsoluteMonthly(
                Period.New(JAN1_2023_UTC.Add("6:00"), JAN1_2023_UTC.Add("7:00")),
                "UTC",
                dayOfMonth: 1)
            .ToBeEquivalent(Period.New(JAN1_2023_UTC.Add("6:00"), JAN1_2023_UTC.Add("7:00")));
    }

    [Test]
    public void WhenNoMatch_ShouldBeEmpty()
    {
        new RepeatEventCalculatorScenario()
            .WithRange(JAN1_2023_UTC, JAN1_2023_UTC.Add("0:00:00.001"))
            .WithAbsoluteMonthly(
                Period.New(JAN1_2023_UTC.AddDays(1).Add("6:00"), JAN1_2023_UTC.AddDays(1).Add("7:00")),
                "UTC",
                dayOfMonth: 2)
            .ToBeEquivalent(Array.Empty<Period<DateTimeOffset>>());
    }

    [Test]
    public void WhenDateMatchButLaterThanTime_ShouldBeEmpty()
    {
        new RepeatEventCalculatorScenario()
            .WithRange(JAN1_2023_UTC.Add("7:00"), JAN1_2023_UTC.Add("7:01"))
            .WithAbsoluteMonthly(
                Period.New(JAN1_2023_UTC.Add("6:00"), JAN1_2023_UTC.Add("7:00")),
                "UTC",
                dayOfMonth: 1)
            .ToBeEquivalent(Array.Empty<Period<DateTimeOffset>>());
    }

    [Test]
    public void WhenDateMatchAndEndLaterThanTime_ShouldBeOk()
    {
        new RepeatEventCalculatorScenario()
            .WithRange(JAN1_2023_UTC.AddDays(-1), JAN1_2023_UTC.Add("6:01"))
            .WithAbsoluteMonthly(
                Period.New(JAN1_2023_UTC.Add("6:00"), JAN1_2023_UTC.Add("7:00")),
                "UTC",
                dayOfMonth: 1)
            .ToBeEquivalent(Period.New(JAN1_2023_UTC.Add("6:00"), JAN1_2023_UTC.Add("7:00")));
    }

    [Test]
    public void WhenMatchMultiple_ShouldBeOk()
    {
        new RepeatEventCalculatorScenario()
            .WithRange(JAN1_2023_UTC.Add("6:00"), JAN1_2023_UTC.AddMonths(1).Add("6:01"))
            .WithAbsoluteMonthly(
                Period.New(JAN1_2023_UTC.Add("6:00"), JAN1_2023_UTC.Add("7:00")),
                "UTC",
                dayOfMonth: 1)
            .ToBeEquivalent(
                Period.New(JAN1_2023_UTC.Add("6:00"), JAN1_2023_UTC.Add("7:00")),
                Period.New(JAN1_2023_UTC.AddMonths(1).Add("6:00"), JAN1_2023_UTC.AddMonths(1).Add("7:00")));
    }

    [Test]
    public void WhenMatchPreviousDayAndOvernightDurationGreaterThanStart_ShouldMatch()
    {
        new RepeatEventCalculatorScenario()
            .WithRange(JAN1_2023_UTC, JAN1_2023_UTC.Add("0:01"))
            .WithAbsoluteMonthly(
                Period.New(JAN1_2023_UTC.AddHours(-1), JAN1_2023_UTC.AddMinutes(1)),
                "UTC",
                dayOfMonth: 31)
            .ToBeEquivalent(Period.New(JAN1_2023_UTC.AddHours(-1), JAN1_2023_UTC.AddMinutes(1)));
    }
    
    [Test]
    public void WhenMidnight_ShouldMatch()
    {
        new RepeatEventCalculatorScenario()
            .WithRange(DateTimeOffset.Parse("2024-08-23T00:00:00+000"), DateTimeOffset.Parse("2024-08-28T00:00:00+000"))
            .WithAbsoluteMonthly(
                Period.New(
                    DateTimeOffset.Parse("2024-08-25T23:00:00+000"),
                    DateTimeOffset.Parse("2024-08-26T00:00:00+000")),
                "Europe/London",
                dayOfMonth: 26)
            .ToBeEquivalent(
                Period.New(
                    DateTimeOffset.Parse("2024-08-25T23:00:00+000"),
                    DateTimeOffset.Parse("2024-08-26T00:00:00+000")));
    }
}
