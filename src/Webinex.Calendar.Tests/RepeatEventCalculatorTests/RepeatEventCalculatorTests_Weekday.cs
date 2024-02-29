using System;
using NUnit.Framework;
using Webinex.Calendar.Common;

namespace Webinex.Calendar.Tests.RepeatEventCalculatorTests;

// ReSharper disable once InconsistentNaming
public class RepeatEventCalculatorTests_Weekday
{
    [Test]
    public void WhenMatchOne_ShouldBeOk()
    {
        new RepeatEventCalculatorScenario()
            .WithRange(JAN1_2023_UTC.Add("6:00"), JAN1_2023_UTC.Add("6:01"))
            .WithWeekdayMatch("6:00", "1:00", "UTC", Weekday.Sunday)
            .ToBeEquivalent(new Period(JAN1_2023_UTC.Add("6:00"), JAN1_2023_UTC.Add("7:00")));
    }

    [Test]
    public void WhenNoMatch_ShouldBeEmpty()
    {
        new RepeatEventCalculatorScenario()
            .WithRange(JAN1_2023_UTC.Add("6:00"), JAN1_2023_UTC.Add("6:00:00.001"))
            .WithWeekdayMatch("6:00", "1:00", "UTC", Weekday.Monday)
            .ToBeEquivalent(Array.Empty<Period>());
    }

    [Test]
    public void WhenDateMatchButLaterThanTime_ShouldBeEmpty()
    {
        new RepeatEventCalculatorScenario()
            .WithRange(JAN1_2023_UTC.Add("7:00"), JAN1_2023_UTC.Add("7:01"))
            .WithWeekdayMatch("6:00", "1:00", "UTC", Weekday.Sunday)
            .ToBeEquivalent(Array.Empty<Period>());
    }

    [Test]
    public void WhenDateMatchAndEndLaterThanTime_ShouldBeOk()
    {
        new RepeatEventCalculatorScenario()
            .WithRange(JAN1_2023_UTC.AddDays(-1), JAN1_2023_UTC.Add("6:01"))
            .WithWeekdayMatch("6:00", "1:00", "UTC", Weekday.Sunday)
            .ToBeEquivalent(new Period(JAN1_2023_UTC.Add("6:00"), JAN1_2023_UTC.Add("7:00")));
    }

    [Test]
    public void WhenMatchMultiple_ShouldBeOk()
    {
        new RepeatEventCalculatorScenario()
            .WithRange(JAN1_2023_UTC.Add("6:00"), JAN1_2023_UTC.AddDays(7).Add("6:01"))
            .WithWeekdayMatch("6:00", "1:00", "UTC", Weekday.Sunday)
            .ToBeEquivalent(
                new Period(JAN1_2023_UTC.Add("6:00"), JAN1_2023_UTC.Add("7:00")),
                new Period(JAN1_2023_UTC.AddDays(7).Add("6:00"), JAN1_2023_UTC.AddDays(7).Add("7:00")));
    }

    [Test]
    public void WhenMatchPreviousDayAndOvernightDurationGreaterThanStart_ShouldMatch()
    {
        new RepeatEventCalculatorScenario()
            .WithRange(JAN1_2023_UTC, JAN1_2023_UTC.Add("0:01"))
            .WithWeekdayMatch("23:00", "1:01", "UTC", Weekday.Saturday)
            .ToBeEquivalent(new Period(JAN1_2023_UTC.AddHours(-1), JAN1_2023_UTC.AddMinutes(1)));
    }

    [Test]
    public void WhenOverStartOfDst_ShouldMatch()
    {
        new RepeatEventCalculatorScenario()
            .WithRange(DateTimeOffset.Parse("2023-10-22T00:00:00+000"), DateTimeOffset.Parse("2023-11-05T02:30:00+000"))
            .WithWeekdayMatch("01:30", "1:00", "Europe/London", Weekday.Sunday)
            .ToBeEquivalent(
                new Period(
                    DateTimeOffset.Parse("2023-10-22T00:30:00+000"),
                    DateTimeOffset.Parse("2023-10-22T01:30:00+000")),
                new Period(
                    DateTimeOffset.Parse("2023-10-29T00:30:00+000"),
                    DateTimeOffset.Parse("2023-10-29T01:30:00+000")),
                new Period(
                    DateTimeOffset.Parse("2023-11-05T01:30:00+000"),
                    DateTimeOffset.Parse("2023-11-05T02:30:00+000")));
    }

    [Test]
    public void WhenOverEndOfDst_ShouldMatch()
    {
        new RepeatEventCalculatorScenario()
            .WithRange(DateTimeOffset.Parse("2024-03-24T01:30:00+000"), DateTimeOffset.Parse("2024-04-07T02:30:00+000"))
            .WithWeekdayMatch("01:30", "1:00", "Europe/London", Weekday.Sunday)
            .ToBeEquivalent(
                new Period(
                    DateTimeOffset.Parse("2024-03-24T01:30:00+000"),
                    DateTimeOffset.Parse("2024-03-24T02:30:00+000")),
                new Period(
                    DateTimeOffset.Parse("2024-03-31T01:30:00+000"),
                    DateTimeOffset.Parse("2024-03-31T02:30:00+000")),
                new Period(
                    DateTimeOffset.Parse("2024-04-07T00:30:00+000"),
                    DateTimeOffset.Parse("2024-04-07T01:30:00+000")));
    }

    [Test]
    public void WhenHasInterval_ShouldMatch()
    {
        new RepeatEventCalculatorScenario()
            .WithRange(DateTimeOffset.Parse("2023-11-06T00:00:00+000"), DateTimeOffset.Parse("2023-11-23T00:00:00+000"))
            .WithWeekdayMatch("01:00", "1:00", "Europe/London", 2, DateTimeOffset.Parse("2023-11-06T01:00:00+000"),
                null, Weekday.Monday, Weekday.Tuesday, Weekday.Wednesday)
            .ToBeEquivalent(
                new Period(
                    DateTimeOffset.Parse("2023-11-06T01:00:00+000"),
                    DateTimeOffset.Parse("2023-11-06T02:00:00+000")),
                new Period(
                    DateTimeOffset.Parse("2023-11-07T01:00:00+000"),
                    DateTimeOffset.Parse("2023-11-07T02:00:00+000")),
                new Period(
                    DateTimeOffset.Parse("2023-11-08T01:00:00+000"),
                    DateTimeOffset.Parse("2023-11-08T02:00:00+000")),
                new Period(
                    DateTimeOffset.Parse("2023-11-20T01:00:00+000"),
                    DateTimeOffset.Parse("2023-11-20T02:00:00+000")),
                new Period(
                    DateTimeOffset.Parse("2023-11-21T01:00:00+000"),
                    DateTimeOffset.Parse("2023-11-21T02:00:00+000")),
                new Period(
                    DateTimeOffset.Parse("2023-11-22T01:00:00+000"),
                    DateTimeOffset.Parse("2023-11-22T02:00:00+000")));
    }
}