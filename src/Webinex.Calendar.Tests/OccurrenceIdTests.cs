using System;
using FluentAssertions;
using NUnit.Framework;

namespace Webinex.Calendar.Tests;

public class OccurrenceIdTests
{
    [Test]
    public void WhenStartIsNull_ShouldRoundTrip()
    {
        var id = OccurrenceId.OneTime(EventId.New(EventType.OneTime));

        var parsed = OccurrenceId.Parse(id.ToString());

        parsed.Should().Be(id);
        parsed.Start.Should().BeNull();
    }

    [Test]
    public void WhenStartSpecified_ShouldKeepFormat()
    {
        var eventId = EventId.New(EventType.Recurrent);
        var start = DateTimeOffset.Parse("2026-10-28T16:00:00Z");
        var id = OccurrenceId.Recurrent(eventId, start);

        var parsed = OccurrenceId.Parse(id.ToString());

        parsed.EventId.Should().Be(eventId);
        parsed.Start.Should().Be(start);
        id.ToString().Length.Should().Be(OccurrenceId.OneTime(EventId.New(EventType.OneTime)).ToString().Length);
    }

    [Test]
    public void WhenStartIsNull_RequiredStartShouldThrow()
    {
        var id = OccurrenceId.OneTime(EventId.New(EventType.OneTime));

        id.Invoking(x => x.GetStartOrThrow()).Should().Throw<InvalidOperationException>();
    }
}
