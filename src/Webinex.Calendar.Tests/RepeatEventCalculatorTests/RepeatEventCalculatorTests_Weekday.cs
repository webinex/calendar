using System;
using NUnit.Framework;
using Webinex.Calendar.Common;
using Webinex.Calendar.Repeats;

namespace Webinex.Calendar.Tests.RepeatEventCalculatorTests;

// ReSharper disable once InconsistentNaming
public class RepeatEventCalculatorTests_Weekday
{
    [Test]
    public void WhenMatchOne_ShouldBeOk()
    {
        new RepeatEventCalculatorScenario()
            .WithRange(JAN1_2023_UTC.Add("6:00"), JAN1_2023_UTC.Add("6:01"))
            .WithWeekdayMatch("6:00", "1:00", Weekday.Sunday)
            .ToBeEquivalent(new Period(JAN1_2023_UTC.Add("6:00"), JAN1_2023_UTC.Add("7:00")));
    }

    [Test]
    public void WhenNoMatch_ShouldBeEmpty()
    {
        new RepeatEventCalculatorScenario()
            .WithRange(JAN1_2023_UTC.Add("6:00"), JAN1_2023_UTC.Add("6:00:00.001"))
            .WithWeekdayMatch("6:00", "1:00", Weekday.Monday)
            .ToBeEquivalent(Array.Empty<Period>());
    }

    [Test]
    public void WhenDateMatchButLaterThanTime_ShouldBeEmpty()
    {
        new RepeatEventCalculatorScenario()
            .WithRange(JAN1_2023_UTC.Add("7:00"), JAN1_2023_UTC.Add("7:01"))
            .WithWeekdayMatch("6:00", "1:00", Weekday.Sunday)
            .ToBeEquivalent(Array.Empty<Period>());
    }

    [Test]
    public void WhenDateMatchAndEndLaterThanTime_ShouldBeOk()
    {
        new RepeatEventCalculatorScenario()
            .WithRange(JAN1_2023_UTC.AddDays(-1), JAN1_2023_UTC.Add("6:01"))
            .WithWeekdayMatch("6:00", "1:00", Weekday.Sunday)
            .ToBeEquivalent(new Period(JAN1_2023_UTC.Add("6:00"), JAN1_2023_UTC.Add("7:00")));
    }

    [Test]
    public void WhenMatchMultiple_ShouldBeOk()
    {
        new RepeatEventCalculatorScenario()
            .WithRange(JAN1_2023_UTC.Add("6:00"), JAN1_2023_UTC.AddDays(7).Add("6:01"))
            .WithWeekdayMatch("6:00", "1:00", Weekday.Sunday)
            .ToBeEquivalent(
                new Period(JAN1_2023_UTC.Add("6:00"), JAN1_2023_UTC.Add("7:00")),
                new Period(JAN1_2023_UTC.AddDays(7).Add("6:00"), JAN1_2023_UTC.AddDays(7).Add("7:00")));
    }

    [Test]
    public void WhenMatchPreviousDayAndOvernightDurationGreaterThanStart_ShouldMatch()
    {
        new RepeatEventCalculatorScenario()
            .WithRange(JAN1_2023_UTC, JAN1_2023_UTC.Add("0:01"))
            .WithWeekdayMatch("23:00", "1:01", Weekday.Saturday)
            .ToBeEquivalent(new Period(JAN1_2023_UTC.AddHours(-1), JAN1_2023_UTC.AddMinutes(1)));
    }
}