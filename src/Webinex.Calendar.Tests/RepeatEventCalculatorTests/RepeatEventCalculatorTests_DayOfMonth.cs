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
            .WithDayOfMonthMatch(timeOfTheDay: "6:00", duration: "1:00", dayOfMonth: 1)
            .ToBeEquivalent(new Period(JAN1_2023_UTC.Add("6:00"), JAN1_2023_UTC.Add("7:00")));
    }

    [Test]
    public void WhenNoMatch_ShouldBeEmpty()
    {
        new RepeatEventCalculatorScenario()
            .WithRange(JAN1_2023_UTC, JAN1_2023_UTC.Add("0:00:00.001"))
            .WithDayOfMonthMatch(timeOfTheDay: "6:00", duration: "1:00", dayOfMonth: 2)
            .ToBeEquivalent(Array.Empty<Period>());
    }

    [Test]
    public void WhenDateMatchButLaterThanTime_ShouldBeEmpty()
    {
        new RepeatEventCalculatorScenario()
            .WithRange(JAN1_2023_UTC.Add("7:00"), JAN1_2023_UTC.Add("7:01"))
            .WithDayOfMonthMatch("6:00", "1:00", dayOfMonth: 1)
            .ToBeEquivalent(Array.Empty<Period>());
    }

    [Test]
    public void WhenDateMatchAndEndLaterThanTime_ShouldBeOk()
    {
        new RepeatEventCalculatorScenario()
            .WithRange(JAN1_2023_UTC.AddDays(-1), JAN1_2023_UTC.Add("6:01"))
            .WithDayOfMonthMatch(timeOfTheDay: "6:00", duration: "1:00", dayOfMonth: 1)
            .ToBeEquivalent(new Period(JAN1_2023_UTC.Add("6:00"), JAN1_2023_UTC.Add("7:00")));
    }

    [Test]
    public void WhenMatchMultiple_ShouldBeOk()
    {
        new RepeatEventCalculatorScenario()
            .WithRange(JAN1_2023_UTC.Add("6:00"), JAN1_2023_UTC.AddMonths(1).Add("6:01"))
            .WithDayOfMonthMatch(timeOfTheDay: "6:00", duration: "1:00", dayOfMonth: 1)
            .ToBeEquivalent(
                new Period(JAN1_2023_UTC.Add("6:00"), JAN1_2023_UTC.Add("7:00")),
                new Period(JAN1_2023_UTC.AddMonths(1).Add("6:00"), JAN1_2023_UTC.AddMonths(1).Add("7:00")));
    }

    [Test]
    public void WhenMatchPreviousDayAndOvernightDurationGreaterThanStart_ShouldMatch()
    {
        new RepeatEventCalculatorScenario()
            .WithRange(JAN1_2023_UTC, JAN1_2023_UTC.Add("0:01"))
            .WithDayOfMonthMatch(timeOfTheDay: "23:00", duration: "1:01", dayOfMonth: 31)
            .ToBeEquivalent(new Period(JAN1_2023_UTC.AddHours(-1), JAN1_2023_UTC.AddMinutes(1)));
    }
}