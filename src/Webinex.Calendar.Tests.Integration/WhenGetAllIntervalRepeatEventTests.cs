using FluentAssertions;
using Webinex.Calendar.Events;
using Webinex.Calendar.Repeats;
using Webinex.Calendar.Tests.Integration.Setups;

namespace Webinex.Calendar.Tests.Integration;

public class WhenGetAllIntervalRepeatEventTests : IntegrationTestsBase
{
    [Test]
    public async Task WhenMatch_ShouldReturn()
    {
        var @event = RecurrentEvent<EventData>.NewInterval(
            JAN1_2023_UTC,
            null,
            intervalMinutes: (int)TimeSpan.FromHours(1).TotalMinutes,
            durationMinutes: (int)TimeSpan.FromMinutes(30).TotalMinutes,
            new EventData("NAME"));

        await Calendar.AddRecurrentEventAsync(@event);
        await DbContext.SaveChangesAsync();

        var events = await Calendar.GetAllAsync(JAN1_2023_UTC, JAN1_2023_UTC.AddHours(2).AddMilliseconds(1));
        events.Length.Should().Be(3);
    }

    [Test]
    public async Task WhenNotMatch_ShouldBeEmpty()
    {
        var @event = RecurrentEvent<EventData>.NewInterval(
            JAN1_2023_UTC,
            null,
            intervalMinutes: (int)TimeSpan.FromHours(1).TotalMinutes,
            durationMinutes: (int)TimeSpan.FromMinutes(30).TotalMinutes,
            new EventData("NAME"));

        await Calendar.AddRecurrentEventAsync(@event);
        await DbContext.SaveChangesAsync();

        var events = await Calendar.GetAllAsync(JAN1_2023_UTC.AddMinutes(30),
            JAN1_2023_UTC.AddHours(1));

        events.Should().BeEmpty();
    }

    [SetUp]
    public new void SetUp()
    {
        CleanDatabase();
    }
}