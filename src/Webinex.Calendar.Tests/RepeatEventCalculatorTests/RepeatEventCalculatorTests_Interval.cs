using System;
using NUnit.Framework;
using Webinex.Calendar.Common;

namespace Webinex.Calendar.Tests.RepeatEventCalculatorTests;

// ReSharper disable once InconsistentNaming
public class RepeatEventCalculatorTests_Interval
{
    [Test]
    public void WhenMatchOne_ShouldBeOk()
    {
        new RepeatEventCalculatorScenario()
            .WithRange(JAN1_2023_UTC.Add("6:00"), JAN1_2023_UTC.Add("6:01"))
            .WithInterval(JAN1_2023_UTC.Add("5:30"), interval: "4:00", duration: "1:00")
            .ToBeEquivalent(new Period(JAN1_2023_UTC.Add("5:30"), JAN1_2023_UTC.Add("6:30")));
    }

    [Test]
    public void WhenRangeStartAtTheEndOfIntervalDuration_ShouldBeEmpty()
    {
        new RepeatEventCalculatorScenario()
            .WithRange(JAN1_2023_UTC.Add("6:30"), JAN1_2023_UTC.Add("6:31"))
            .WithInterval(JAN1_2023_UTC.Add("5:30"), interval: "1:00:00:00", duration: "1:00")
            .ToBeEquivalent(Array.Empty<Period>());
    }

    [Test]
    public void WhenRangeEndAtTheStartOfInterval_ShouldBeEmpty()
    {
        new RepeatEventCalculatorScenario()
            .WithRange(JAN1_2023_UTC.Add("6:00"), JAN1_2023_UTC.Add("7:00"))
            .WithInterval(JAN1_2023_UTC.Add("7:00"), interval: "1:00:00:00", duration: "1:00")
            .ToBeEquivalent(Array.Empty<Period>());
    }

    [Test]
    public void WhenMatchMultiple_ShouldBeOk()
    {
        new RepeatEventCalculatorScenario()
            .WithRange(JAN1_2023_UTC.Add("6:00"), JAN1_2023_UTC.Add("8:01"))
            .WithInterval(JAN1_2023_UTC.Add("6:00"), interval: "2:00", duration: "1:00")
            .ToBeEquivalent(
                new Period(JAN1_2023_UTC.Add("6:00"), JAN1_2023_UTC.Add("7:00")),
                new Period(JAN1_2023_UTC.Add("8:00"), JAN1_2023_UTC.Add("9:00")));
    }

    [Test]
    public void WhenMatchMultipleSelfCoverIntervals_ShouldBeOk()
    {
        new RepeatEventCalculatorScenario()
            .WithRange(JAN1_2023_UTC.Add("6:00"), JAN1_2023_UTC.Add("6:31"))
            .WithInterval(JAN1_2023_UTC.Add("6:00"), interval: "0:30", duration: "1:00")
            .ToBeEquivalent(
                new Period(JAN1_2023_UTC.Add("6:00"), JAN1_2023_UTC.Add("7:00")),
                new Period(JAN1_2023_UTC.Add("6:30"), JAN1_2023_UTC.Add("7:30")));
    }
}