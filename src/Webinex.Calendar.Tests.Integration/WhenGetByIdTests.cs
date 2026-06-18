using FluentAssertions;
using Webinex.Calendar.Tests.Integration.Setups;

namespace Webinex.Calendar.Tests.Integration;

public class WhenGetByIdTests : IntegrationTestsBase
{
    [Test]
    public async Task WhenRequestedTwice_ShouldReturnSameInstance()
    {
        var @event = Event<EventData>.New(
            Period.New(JAN1_2023_UTC.AddHours(5), JAN1_2023_UTC.AddHours(6)),
            TimeZoneInfo.Utc.Id,
            new EventData("NAME"));

        await Calendar.AddAsync(@event);
        await DbContext.SaveChangesAsync();

        var first = await Calendar.ByIdAsync<Event<EventData>>(@event.Id);
        var second = await Calendar.ByIdAsync<Event<EventData>>(@event.Id);

        second.Should().BeSameAs(first);
    }

    [SetUp]
    public void SetUp()
    {
        CleanDatabase();
    }
}
