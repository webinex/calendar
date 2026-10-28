using FluentAssertions;
using Webinex.Calendar.Extensions;
using Webinex.Calendar.Tests.Integration.Setups;

namespace Webinex.Calendar.Tests.Integration;

public class WhenGetOccurrenceByIdTests : IntegrationTestsBase
{
    [Test]
    public async Task WhenOccurrenceCancelled_ShouldReturnNull()
    {
        var @event = await AddDailyEventAsync();
        var id = OccurrenceId.Recurrent(@event.Id, JAN1_2023_UTC.AddDays(1).AddHours(12)).ToString();

        await Calendar.CancelOccurrenceAsync(id);
        await DbContext.SaveChangesAsync();

        var result = await Calendar.OccurrenceAsync(id);
        result.Should().BeNull();
    }

    [Test]
    public async Task WhenEventNotFound_ShouldReturnNull()
    {
        var notSavedEvent = Event.Factory.MGDaily(
            new Period<DateTimeOffset>(JAN1_2023_UTC.AddHours(12), JAN1_2023_UTC.AddHours(13)),
            TimeZoneInfo.Utc.Id,
            EventData.Test());
        var id = OccurrenceId.Recurrent(notSavedEvent.Id, JAN1_2023_UTC.AddHours(12)).ToString();

        var result = await Calendar.OccurrenceAsync(id);
        result.Should().BeNull();
    }

    [Test]
    public async Task WhenOccurrenceIdDoesntMatchRecurrence_ShouldReturnNull()
    {
        var @event = await AddDailyEventAsync();
        var id = OccurrenceId.Recurrent(@event.Id, JAN1_2023_UTC.AddDays(1).AddHours(12).AddMinutes(30)).ToString();

        var result = await Calendar.OccurrenceAsync(id);
        result.Should().BeNull();
    }

    [Test]
    public async Task WhenSomeOccurrencesMissing_ShouldReturnExisting()
    {
        var @event = await AddDailyEventAsync();
        var existingId = OccurrenceId.Recurrent(@event.Id, JAN1_2023_UTC.AddHours(12)).ToString();
        var missingId = OccurrenceId.Recurrent(@event.Id, JAN1_2023_UTC.AddHours(13)).ToString();

        var result = await Calendar.OccurrencesAsync([existingId, missingId]);
        result.Select(x => x.Id).Should().BeEquivalentTo([existingId]);
    }

    private async Task<Event<EventData>> AddDailyEventAsync()
    {
        var @event = Event.Factory.MGDaily(
            new Period<DateTimeOffset>(JAN1_2023_UTC.AddHours(12), JAN1_2023_UTC.AddHours(13)),
            TimeZoneInfo.Utc.Id,
            EventData.Test());

        await Calendar.AddAsync(@event);
        await DbContext.SaveChangesAsync();
        return @event;
    }

    [SetUp]
    public void SetUp()
    {
        CleanDatabase();
    }
}
