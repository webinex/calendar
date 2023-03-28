using NUnit.Framework;
using Webinex.Calendar.DataAccess;
using Webinex.Calendar.Events;

namespace Webinex.Calendar.Tests.EventFilterFactoryTests;
using static CommonConstants;

// ReSharper disable once InconsistentNaming
public abstract class EventFilterFactoryTests_ExactDateEventBase
{
    protected abstract EventType Type { get; }
    
    [Test]
    public void WhenMatch_ShouldBeOk()
    {
        new EventFilterFactoryScenario()
            .WithRange(JAN1_2023_UTC, JAN1_2023_UTC.AddDays(1))
            .WithExactDateEvent(Type, JAN1_2023_UTC.Add("06:00"), "01:00")
            .ToContainAll();
    }

    [Test]
    public void WhenMatchMultiple_ShouldBeOk()
    {
        new EventFilterFactoryScenario()
            .WithRange(JAN1_2023_UTC, JAN1_2023_UTC.AddDays(1))
            .WithExactDateEvent("MATCH", Type, JAN1_2023_UTC.Add("06:00"), "01:00")
            .WithExactDateEvent("MATCH", Type, JAN1_2023_UTC.Add("06:30"), "01:00")
            .ToContain("MATCH");
    }

    [Test]
    public void WhenSomeAfter_ShouldBeOk()
    {
        new EventFilterFactoryScenario()
            .WithRange(JAN1_2023_UTC, JAN1_2023_UTC.Add("12:00"))
            .WithExactDateEvent("MATCH", Type, JAN1_2023_UTC.Add("06:00"), "01:00")
            .WithExactDateEvent("MATCH", Type, JAN1_2023_UTC.Add("06:30"), "01:00")
            .WithExactDateEvent("NOT_MATCH", Type, JAN1_2023_UTC.Add("12:00"), "01:00")
            .ToContain("MATCH");
    }

    [Test]
    public void WhenSomeBefore_ShouldBeOk()
    {
        new EventFilterFactoryScenario()
            .WithRange(JAN1_2023_UTC.Add("06:00"), JAN1_2023_UTC.Add("12:00"))
            .WithExactDateEvent("MATCH", Type, JAN1_2023_UTC.Add("06:00"), "01:00")
            .WithExactDateEvent("MATCH", Type, JAN1_2023_UTC.Add("06:30"), "01:00")
            .WithExactDateEvent("NOT_MATCH", Type, JAN1_2023_UTC.Add("05:00"), "01:00")
            .ToContain("MATCH");
    }

    [Test]
    public void WhenStartBeforeRangeButEndInRange_ShouldMatch()
    {
        new EventFilterFactoryScenario()
            .WithRange(JAN1_2023_UTC.Add("06:00"), JAN1_2023_UTC.Add("12:00"))
            .WithExactDateEvent("MATCH", Type, JAN1_2023_UTC.Add("05:00"), "01:01")
            .ToContain("MATCH");
    }

    [Test]
    public void WhenStartInRangeButEndOutOfRange_ShouldMatch()
    {
        new EventFilterFactoryScenario()
            .WithRange(JAN1_2023_UTC.Add("06:00"), JAN1_2023_UTC.Add("12:00"))
            .WithExactDateEvent("MATCH", Type, JAN1_2023_UTC.Add("11:30"), "00:31")
            .ToContain("MATCH");
    }

    [Test]
    public void WhenStartAtEndOfRange_ShouldNotMatch()
    {
        new EventFilterFactoryScenario()
            .WithRange(JAN1_2023_UTC.Add("06:00"), JAN1_2023_UTC.Add("12:00"))
            .WithExactDateEvent(Type, JAN1_2023_UTC.Add("12:00"), "00:01")
            .ToBeEmpty();
    }

    [Test]
    public void WhenEndAtStartOfRange_ShouldNotMatch()
    {
        new EventFilterFactoryScenario()
            .WithRange(JAN1_2023_UTC.Add("06:00"), JAN1_2023_UTC.Add("12:00"))
            .WithExactDateEvent(Type, JAN1_2023_UTC.Add("05:00"), "01:00")
            .ToBeEmpty();
    }

    [Test]
    public void WhenStartAndEndMatchRange_ShouldMatch()
    {
        new EventFilterFactoryScenario()
            .WithRange(JAN1_2023_UTC.Add("06:00"), JAN1_2023_UTC.Add("12:00"))
            .WithExactDateEvent("MATCH", Type, JAN1_2023_UTC.Add("06:00"), "06:00")
            .ToContain("MATCH");
    }
}