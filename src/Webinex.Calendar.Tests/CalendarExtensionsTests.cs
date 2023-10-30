using System;
using System.Linq;
using FluentAssertions;
using Ical.Net;
using Ical.Net.CalendarComponents;
using Ical.Net.DataTypes;
using NUnit.Framework;
using Webinex.Calendar.Common;

namespace Webinex.Calendar.Tests;

public class CalendarExtensionsTests
{
    [Test]
    public void ShouldWork()
    {
        var start = DateTimeOffset.Parse("2023-10-30T01:00:00+000");
        
        var calendarEvent = new CalendarEvent
        {
            Start = new CalDateTime(start.DateTime, "UTC"),
            End = new CalDateTime(start.AddHours(1).DateTime, "UTC"),
            RecurrenceRules =
            {
                new RecurrencePattern(FrequencyType.Monthly),
            },
        };

        var calendar = new Ical.Net.Calendar();
        calendar.Events.Add(calendarEvent);

        var events = calendar.GetOccurrencesEnumerable(
            new CalDateTime(start.DateTime),
            null);

        var result = events.TakeWhile(x => x.Period.StartTime.AsDateTimeOffset <= start.AddYears(1));

        result.Count().Should().Be(12);
    }
}