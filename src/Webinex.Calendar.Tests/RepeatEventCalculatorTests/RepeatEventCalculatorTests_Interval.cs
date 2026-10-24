using System;
using NUnit.Framework;

namespace Webinex.Calendar.Tests.RepeatEventCalculatorTests;

// ReSharper disable once InconsistentNaming
public class RepeatEventCalculatorTests_Interval
{
    [Test]
    public void WhenRangeStartAtTheEndOfIntervalDuration_ButEndsAfterOneInterval_ShouldBeOk()
    {
        var jan2Of2023Utc = JAN1_2023_UTC.AddDays(1);
        
        new RepeatEventCalculatorScenario()
            .WithRange(JAN1_2023_UTC.Add("6:30"), jan2Of2023Utc.Add("6:30"))
            .WithInterval(JAN1_2023_UTC.Add("5:30"), interval: "1:00:00:00", duration: "1:00")
            .ToBeEquivalent(Period.New(jan2Of2023Utc.Add("5:30"), jan2Of2023Utc.Add("6:30")));
    }

    [Test]
    public void WhenRangeStartAtTheEndOfIntervalDuration_ShouldBeEmpty()
    {
        new RepeatEventCalculatorScenario()
            .WithRange(JAN1_2023_UTC.Add("6:30"), JAN1_2023_UTC.Add("6:31"))
            .WithInterval(JAN1_2023_UTC.Add("5:30"), interval: "1:00:00:00", duration: "1:00")
            .ToBeEquivalent(Array.Empty<Period<DateTimeOffset>>());
    }

    [Test]
    public void WhenRangeEndAtTheStartOfInterval_ShouldBeEmpty()
    {
        new RepeatEventCalculatorScenario()
            .WithRange(JAN1_2023_UTC.Add("6:00"), JAN1_2023_UTC.Add("7:00"))
            .WithInterval(JAN1_2023_UTC.Add("7:00"), interval: "1:00:00:00", duration: "1:00")
            .ToBeEquivalent(Array.Empty<Period<DateTimeOffset>>());
    }
}
