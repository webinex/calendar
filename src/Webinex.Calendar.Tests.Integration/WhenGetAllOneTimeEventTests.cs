using FluentAssertions;
using Webinex.Calendar.Events;
using Webinex.Calendar.Tests.Integration.Setups;

namespace Webinex.Calendar.Tests.Integration;

public class WhenGetAllOneTimeEventTests : IntegrationTestsBase
{
    [Test]
    public async Task WhenMatch_ShouldReturn()
    {
        await Calendar.AddOneTimeEventAsync(OneTimeEvent<EventData>.New(JAN1_2023_UTC.AddHours(5), JAN1_2023_UTC.AddHours(6),
            new EventData("NAME")));

        await DbContext.SaveChangesAsync();

        var events = await Calendar.GetAllAsync(JAN1_2023_UTC, JAN1_2023_UTC.AddDays(1));
        events.Length.Should().Be(1);
    }

    [Test]
    public async Task WhenNotMatch_ShouldBeEmpty()
    {
        await Calendar.AddOneTimeEventAsync(OneTimeEvent<EventData>.New(JAN1_2023_UTC.AddHours(5), JAN1_2023_UTC.AddHours(6),
            new EventData("NAME")));

        await DbContext.SaveChangesAsync();

        var events = await Calendar.GetAllAsync(JAN1_2023_UTC.AddHours(6), JAN1_2023_UTC.AddDays(1));
        events.Should().BeEmpty();
    }

    [SetUp]
    public new void SetUp()
    {
        CleanDatabase();
    }
}