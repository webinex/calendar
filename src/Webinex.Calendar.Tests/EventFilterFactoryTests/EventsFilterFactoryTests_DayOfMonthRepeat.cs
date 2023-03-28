using NUnit.Framework;

namespace Webinex.Calendar.Tests.EventFilterFactoryTests;

// ReSharper disable once InconsistentNaming
public class EventsFilterFactoryTests_DayOfMonthRepeat
{
    [Test]
    public void WhenMatch_ShouldContain()
    {
        new EventFilterFactoryScenario()
            .WithRange(JAN1_2023_UTC, JAN1_2023_UTC.AddDays(1))
            .WithDayOfMonthRepeatEvent("6:00", "1:00", 1)
            .ToContainAll();
    }

    [Test]
    public void WhenStartAtTheEndOfRange_ShouldBeEmpty()
    {
        new EventFilterFactoryScenario()
            .WithRange(JAN1_2023_UTC, JAN1_2023_UTC.AddDays(1))
            .WithDayOfMonthRepeatEvent("0:00", "1:00", 2)
            .ToBeEmpty();
    }

    [Test]
    public void WhenEndsAtStartOfRange_ShouldBeEmpty()
    {
        new EventFilterFactoryScenario()
            .WithRange(JAN1_2023_UTC, JAN1_2023_UTC.AddDays(1))
            .WithDayOfMonthRepeatEvent("23:00", "1:00", 31)
            .ToBeEmpty();
    }

    [Test]
    public void WhenStartsBeforeRangeDateButRangeDateInDuration_ShouldContain()
    {
        new EventFilterFactoryScenario()
            .WithRange(JAN1_2023_UTC, JAN1_2023_UTC.AddDays(1))
            .WithDayOfMonthRepeatEvent("23:00", "2:00", 31)
            .ToContainAll();
    }

    [Test]
    public void WhenDateMatchWithRangeEndButStartsAfterRange_ShouldBeEmpty()
    {
        new EventFilterFactoryScenario()
            .WithRange(JAN1_2023_UTC, JAN1_2023_UTC.AddDays(1).AddHours(16))
            .WithDayOfMonthRepeatEvent("16:00", "2:00", 2)
            .ToBeEmpty();
    }

    [Test]
    public void WhenDateMatchWithRangeStartButEndsBeforeRangeStart_ShouldBeEmpty()
    {
        new EventFilterFactoryScenario()
            .WithRange(JAN1_2023_UTC.AddHours(6), JAN1_2023_UTC.AddDays(1).AddHours(16))
            .WithDayOfMonthRepeatEvent("2:00", "2:00", 1)
            .ToBeEmpty();
    }
}