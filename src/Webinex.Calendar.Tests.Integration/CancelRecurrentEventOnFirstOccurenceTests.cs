using Webinex.Calendar.Extensions;
using Webinex.Calendar.Tests.Integration.Setups;

namespace Webinex.Calendar.Tests.Integration;

[TestFixture]
public class CancelRecurrentEventOnFirstOccurenceTests : IntegrationTestsBase
{
    [Test]
    public async Task ShouldBeOk()
    {
        var start = DateTimeOffset.Parse("2022-01-01T01:00:00Z");
        
        var @event = Event.Factory.MGDaily(
            Period.New(start, start.AddHours(1)),
            TimeZoneInfo.Utc.Id,
            EventData.Test());

        await Calendar.AddAsync(@event);
        await DbContext.SaveChangesAsync();

        var occurences = await Calendar.OccurrencesAsync(start, start.AddMinutes(1));
        await Calendar.CancelOccurrenceAsync(occurences.First().Id, OccurenceUpdateBehavior.Group);
    }
}