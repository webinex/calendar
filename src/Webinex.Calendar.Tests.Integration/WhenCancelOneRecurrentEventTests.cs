using FluentAssertions;
using Webinex.Calendar.Extensions;
using Webinex.Calendar.Tests.Integration.Setups;

namespace Webinex.Calendar.Tests.Integration;

public class WhenCancelOneRecurrentEventTests : IntegrationTestsBase
{
    [Test]
    public async Task WhenCancel_ShouldExcept()
    {
        var @event = Event.Factory.MGDaily(
            new Period<DateTimeOffset>(JAN1_2023_UTC.AddHours(12), JAN1_2023_UTC.AddHours(13)),
            TimeZoneInfo.Utc.Id,
            EventData.Test());

        await Calendar.AddAsync(@event);
        await DbContext.SaveChangesAsync();

        var eventsBefore = await Calendar.OccurrencesAsync(JAN1_2023_UTC, JAN1_2023_UTC.AddDays(2));
        eventsBefore.Count.Should().Be(2);

        await Calendar.CancelOccurrenceAsync(eventsBefore.ElementAt(0).Id);
        await DbContext.SaveChangesAsync();

        var eventsAfter = await Calendar.OccurrencesAsync(JAN1_2023_UTC, JAN1_2023_UTC.AddDays(2));
        eventsAfter.Count.Should().Be(1);
        eventsAfter.Single().Period.Start.Should().Be(JAN1_2023_UTC.AddDays(1).AddHours(12));
    }

    [Test]
    public async Task WhenDailyKiritimatiOccurrenceIsCancelledAsGroup_ShouldRemoveAllOccurrencesSinceLocalDate()
    {
        var @event = Event.Factory.MGDaily(
            Period.New(
                DateTimeOffset.Parse("2026-06-20T18:00:00+00:00"),
                DateTimeOffset.Parse("2026-06-20T19:00:00+00:00")),
            "Pacific/Kiritimati",
            EventData.Test());

        await Calendar.AddAsync(@event);
        await DbContext.SaveChangesAsync();

        var eventsBefore = await Calendar.OccurrencesAsync(
            DateTimeOffset.Parse("2026-06-20T21:00:00+00:00"),
            DateTimeOffset.Parse("2026-06-26T21:00:00+00:00"));
        eventsBefore.Count.Should().Be(6);

        var occurrenceToCancel = eventsBefore.Single(x =>
            x.Period.Start == DateTimeOffset.Parse("2026-06-21T18:00:00+00:00"));
        await Calendar.CancelOccurrenceAsync(occurrenceToCancel.Id, OccurenceUpdateBehavior.Group);
        await DbContext.SaveChangesAsync();

        var eventsAfter = await Calendar.OccurrencesAsync(
            DateTimeOffset.Parse("2026-06-20T21:00:00+00:00"),
            DateTimeOffset.Parse("2026-06-26T21:00:00+00:00"));
        eventsAfter.Count.Should().Be(0);
    }

    [Test]
    public async Task WhenDailyKiritimatiOccurrenceIsCancelledAsGroup_ShouldKeepOnlyPreviousOccurrence()
    {
        var @event = Event.Factory.MGDaily(
            Period.New(
                DateTimeOffset.Parse("2026-06-20T18:00:00+00:00"),
                DateTimeOffset.Parse("2026-06-20T19:00:00+00:00")),
            "Pacific/Kiritimati",
            EventData.Test());

        await Calendar.AddAsync(@event);
        await DbContext.SaveChangesAsync();

        var eventsBefore = await Calendar.OccurrencesAsync(
            DateTimeOffset.Parse("2026-06-20T17:00:00+00:00"),
            DateTimeOffset.Parse("2026-06-26T21:00:00+00:00"));
        eventsBefore.Count.Should().Be(7);

        var occurrenceToCancel = eventsBefore.Single(x =>
            x.Period.Start == DateTimeOffset.Parse("2026-06-21T18:00:00+00:00"));
        await Calendar.CancelOccurrenceAsync(occurrenceToCancel.Id, OccurenceUpdateBehavior.Group);
        await DbContext.SaveChangesAsync();

        var eventsAfter = await Calendar.OccurrencesAsync(
            DateTimeOffset.Parse("2026-06-20T17:00:00+00:00"),
            DateTimeOffset.Parse("2026-06-26T21:00:00+00:00"));
        eventsAfter.Should().ContainSingle();
        eventsAfter.Single().Period.Start.Should().Be(DateTimeOffset.Parse("2026-06-20T18:00:00+00:00"));
    }

    [SetUp]
    public void SetUp()
    {
        CleanDatabase();
    }
}
