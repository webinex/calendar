using FluentAssertions;
using Webinex.Calendar.Extensions;
using Webinex.Calendar.Tests.Integration.Setups;

namespace Webinex.Calendar.Tests.Integration;

public class WhenGetAllWeeklyRepeatEventTests : IntegrationTestsBase
{
    [Test]
    public async Task WhenMatch_ShouldReturn()
    {
        var @event = Event.Factory.MGWeekly(
            Period.New(JAN1_2023_UTC.AddHours(6), JAN1_2023_UTC.AddHours(6).AddHours(1)),
            TimeZoneInfo.Utc.Id,
            EventData.Test(),
            [DayOfWeek.Sunday, DayOfWeek.Tuesday]);

        await Calendar.AddAsync(@event);
        await DbContext.SaveChangesAsync();

        var events = await Calendar.OccurrencesAsync(JAN1_2023_UTC, JAN1_2023_UTC.AddDays(2).AddHours(6).AddMinutes(1));
        events.Count.Should().Be(2);
    }

    [Test]
    public async Task WhenNotMatch_ShouldBeEmpty()
    {
        var @event = Event.Factory.MGWeekly(
            Period.New(JAN1_2023_UTC.AddHours(6), JAN1_2023_UTC.AddHours(6).AddHours(1)),
            TimeZoneInfo.Utc.Id,
            EventData.Test(),
            [DayOfWeek.Sunday, DayOfWeek.Tuesday]);

        await Calendar.AddAsync(@event);
        await DbContext.SaveChangesAsync();

        var events = await Calendar.OccurrencesAsync(JAN1_2023_UTC.AddDays(2).AddHours(7), JAN1_2023_UTC.AddDays(3));
        events.Should().BeEmpty();
    }

    [SetUp]
    public void SetUp()
    {
        CleanDatabase();
    }
}