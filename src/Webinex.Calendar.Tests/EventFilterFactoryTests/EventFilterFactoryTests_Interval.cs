using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using NUnit.Framework;

namespace Webinex.Calendar.Tests.EventFilterFactoryTests;

// ReSharper disable once InconsistentNaming
public class EventFilterFactoryTests_Interval
{
    [Test]
    public void WhenMatch_ShouldBeOk()
    {
        new EventFilterFactoryScenario()
            .WithRange(JAN1_2023_UTC, JAN1_2023_UTC.Add("6:00"))
            .WithIntervalRepeatEvent(JAN1_2023_UTC.Add("5:59"), interval: "1:00", duration: "1:00")
            .ToContainAll();
    }

    [Test]
    public void WhenNotMatch_ShouldBeOk()
    {
        new EventFilterFactoryScenario()
            .WithRange(JAN1_2023_UTC.Add("6:00"), JAN1_2023_UTC.Add("7:00"))
            .WithIntervalRepeatEvent(JAN1_2023_UTC.Add("5:30"), interval: "2:00", duration: "0:30")
            .ToBeEmpty();
    }

    [Test]
    public void WhenMatchMultiple_ShouldBeOk()
    {
        new EventFilterFactoryScenario()
            .WithRange(JAN1_2023_UTC.Add("6:00"), JAN1_2023_UTC.Add("8:00"))
            .WithIntervalRepeatEvent("MATCH", JAN1_2023_UTC.Add("6:00"), interval: "2:00", duration: "0:30")
            .WithIntervalRepeatEvent("MATCH", JAN1_2023_UTC.Add("4:00"), interval: "2:00", duration: "0:30")
            .WithIntervalRepeatEvent("NO_MATCH", JAN1_2023_UTC.Add("8:00"), interval: "2:00", duration: "0:30")
            .ToContain("MATCH");
    }

    [Test]
    public void WhenStartedBeforeRangeButDurationInRange_ShouldMatch()
    {
        new EventFilterFactoryScenario()
            .WithRange(JAN1_2023_UTC.Add("6:00"), JAN1_2023_UTC.Add("7:00"))
            .WithIntervalRepeatEvent(JAN1_2023_UTC.Add("5:30"), interval: "15:00", duration: "01:00:00.001")
            .ToContainAll();
    }

    [Test]
    public void WhenEventExactMatchRange_ShouldBeOk()
    {
        new EventFilterFactoryScenario()
            .WithRange(JAN1_2023_UTC.Add("6:00"), JAN1_2023_UTC.Add("7:00"))
            .WithIntervalRepeatEvent(JAN1_2023_UTC.Add("6:00"), interval: "15:00", duration: "1:00")
            .ToContainAll();
    }

    [Test]
    public void WhenInsidePeriod_ShouldBeEmpty()
    {
        new EventFilterFactoryScenario()
            .WithRange(JAN1_2023_UTC, JAN1_2023_UTC.AddMinutes(30))
            .WithIntervalRepeatEvent(JAN1_2023_UTC.AddMinutes(-30), "1:00", "0:30")
            .ToBeEmpty();
    }
}