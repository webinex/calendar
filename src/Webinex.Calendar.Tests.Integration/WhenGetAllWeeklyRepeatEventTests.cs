using FluentAssertions;
using Webinex.Calendar.Events;
using Webinex.Calendar.Repeats;
using Webinex.Calendar.Tests.Integration.Setups;

namespace Webinex.Calendar.Tests.Integration;

public class WhenGetAllWeeklyRepeatEventTests : IntegrationTestsBase
{
    [Test]
    public async Task WhenMatch_ShouldReturn()
    {
        var @event = RecurrentEvent<EventData>.NewMatch(
            (int)TimeSpan.FromHours(6).TotalMinutes,
            (int)TimeSpan.FromHours(1).TotalMinutes,
            new[] { Weekday.Sunday, Weekday.Tuesday },
            null,
            JAN1_2023_UTC,
            null,
            new EventData("NAME"));

        await Calendar.AddRecurrentEventAsync(@event);
        await DbContext.SaveChangesAsync();

        var events = await Calendar.GetAllAsync(JAN1_2023_UTC, JAN1_2023_UTC.AddDays(2).AddHours(6).AddMilliseconds(1));
        events.Length.Should().Be(2);
    }

    [Test]
    public async Task WhenNotMatch_ShouldBeEmpty()
    {
        var @event = RecurrentEvent<EventData>.NewMatch(
            (int)TimeSpan.FromHours(6).TotalMinutes,
            (int)TimeSpan.FromHours(1).TotalMinutes,
            new[] { Weekday.Sunday, Weekday.Tuesday },
            null,
            JAN1_2023_UTC,
            null,
            new EventData("NAME"));

        await Calendar.AddRecurrentEventAsync(@event);
        await DbContext.SaveChangesAsync();

        var events = await Calendar.GetAllAsync(JAN1_2023_UTC.AddDays(2).AddHours(7), JAN1_2023_UTC.AddDays(3));
        events.Should().BeEmpty();
    }

    [SetUp]
    public new void SetUp()
    {
        CleanDatabase();
    }
}