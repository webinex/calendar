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

    [SetUp]
    public void SetUp()
    {
        CleanDatabase();
    }
}