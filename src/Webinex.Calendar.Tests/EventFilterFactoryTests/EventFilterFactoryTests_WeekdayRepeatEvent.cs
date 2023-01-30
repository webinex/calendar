using NUnit.Framework;
using Webinex.Calendar.Repeats;

namespace Webinex.Calendar.Tests.EventFilterFactoryTests;

using static CommonConstants;

// ReSharper disable once InconsistentNaming
public class EventFilterFactoryTests_WeekdayRepeatEvent
{
    [Test]
    public void WhenMatch_ShouldBeOk()
    {
        new EventFilterFactoryScenario()
            .WithRange(JAN1_2023_UTC, JAN1_2023_UTC.AddDays(1))
            .WithWeekdayRepeatEvent(timeOfTheDay: "06:00", duration: "00:01", Weekday.Sunday)
            .ToContainAll();
    }

    [Test]
    public void WhenNotMatch_ShouldBeOk()
    {
        new EventFilterFactoryScenario()
            .WithRange(JAN1_2023_UTC, JAN1_2023_UTC.AddDays(1))
            .WithWeekdayRepeatEvent(timeOfTheDay: "06:00", duration: "00:01", Weekday.Saturday)
            .ToBeEmpty();
    }

    [Test]
    public void WhenMatchMultiple_ShouldBeOk()
    {
        new EventFilterFactoryScenario()
            .WithRange(JAN1_2023_UTC, JAN1_2023_UTC.AddDays(3))
            .WithWeekdayRepeatEvent("MATCH", timeOfTheDay: "06:00", duration: "00:01", Weekday.Sunday)
            .WithWeekdayRepeatEvent("MATCH", timeOfTheDay: "06:00", duration: "00:01", Weekday.Tuesday)
            .WithWeekdayRepeatEvent("NOT_MATCH", timeOfTheDay: "06:00", duration: "00:01", Weekday.Thursday)
            .ToContain("MATCH");
    }

    [Test]
    public void WhenOvernightEventEndsInRange_ShouldMatch()
    {
        new EventFilterFactoryScenario()
            .WithRange(JAN1_2023_UTC, JAN1_2023_UTC.Add("00:01"))
            .WithWeekdayRepeatEvent(timeOfTheDay: "23:00", duration: "01:01", Weekday.Saturday)
            .ToContainAll();
    }

    [Test]
    public void WhenOvernightEventEndsAtStartOfRange_ShouldNotMatch()
    {
        new EventFilterFactoryScenario()
            .WithRange(JAN1_2023_UTC, JAN1_2023_UTC.Add("00:01"))
            .WithWeekdayRepeatEvent(timeOfTheDay: "23:00", duration: "01:00", Weekday.Saturday)
            .ToBeEmpty();
    }

    [Test]
    public void WhenOvernightEventEndsBeforeStartOfRange_ShouldBeEmpty()
    {
        new EventFilterFactoryScenario()
            .WithRange(JAN1_2023_UTC.Add("1:00"), JAN1_2023_UTC.Add("7:00"))
            .WithWeekdayRepeatEvent(timeOfTheDay: "23:00", "01:59", Weekday.Saturday)
            .ToBeEmpty();
    }

    [Test]
    public void WhenEventExactMatchRange_ShouldBeOk()
    {
        new EventFilterFactoryScenario()
            .WithRange(JAN1_2023_UTC, JAN1_2023_UTC.AddDays(1))
            .WithWeekdayRepeatEvent(timeOfTheDay: "0:00", duration: "1:00:00", Weekday.Sunday)
            .ToContainAll();
    }

    [Test]
    public void WhenEventMatchByDateButEarlier_ShouldBeEmpty()
    {
        new EventFilterFactoryScenario()
            .WithRange(JAN1_2023_UTC.Add("6:00"), JAN1_2023_UTC.Add("7:00"))
            .WithWeekdayRepeatEvent(timeOfTheDay: "5:00", "00:59", Weekday.Sunday)
            .ToBeEmpty();
    }

    [Test]
    public void WhenEventMatchByDateButLater_ShouldBeEmpty()
    {
        new EventFilterFactoryScenario()
            .WithRange(JAN1_2023_UTC.Add("6:00"), JAN1_2023_UTC.Add("7:00"))
            .WithWeekdayRepeatEvent(timeOfTheDay: "7:01", "00:01", Weekday.Sunday)
            .ToBeEmpty();
    }

    [Test]
    public void WhenEventMatchByDateButEndsAtStartOfRange_ShouldBeEmpty()
    {
        new EventFilterFactoryScenario()
            .WithRange(JAN1_2023_UTC.Add("6:00"), JAN1_2023_UTC.Add("7:00"))
            .WithWeekdayRepeatEvent(timeOfTheDay: "5:00", "01:00", Weekday.Sunday)
            .ToBeEmpty();
    }

    [Test]
    public void WhenEventMatchByDateButStartsAtEndOfRange_ShouldBeEmpty()
    {
        new EventFilterFactoryScenario()
            .WithRange(JAN1_2023_UTC.Add("6:00"), JAN1_2023_UTC.Add("7:00"))
            .WithWeekdayRepeatEvent(timeOfTheDay: "7:00", "01:00", Weekday.Sunday)
            .ToBeEmpty();
    }

    [Test]
    public void WhenLastDayMatchButStartAfterRangeEnd_ShouldBeEmpty()
    {
        new EventFilterFactoryScenario()
            .WithRange(JAN1_2023_UTC.Add("6:00"), JAN1_2023_UTC.AddDays(2).Add("7:00"))
            .WithWeekdayRepeatEvent(timeOfTheDay: "7:01", "01:00", Weekday.Tuesday)
            .ToBeEmpty();
    }
}