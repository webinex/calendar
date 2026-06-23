using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Webinex.Calendar.Extensions;
using Webinex.Calendar.Tests.Integration.Setups;

namespace Webinex.Calendar.Tests.Integration;

public class WhenRepositoryGetAllByEventEntityTypeTests : IntegrationTestsBase
{
    [Test]
    public async Task OneTimeEvent_ShouldNotReturnOccurrenceAdjustments()
    {
        var oneTimeEvent = Event<EventData>.New(
            Period.New(JAN1_2023_UTC.AddHours(5), JAN1_2023_UTC.AddHours(6)),
            TimeZoneInfo.Utc.Id,
            EventData.Test());
        var recurrentEvent = Event.Factory.MGWeekly(
            Period.New(JAN1_2023_UTC.AddHours(6), JAN1_2023_UTC.AddHours(7)),
            TimeZoneInfo.Utc.Id,
            EventData.Test(),
            [DayOfWeek.Sunday]);

        await Calendar.AddAsync(oneTimeEvent);
        await Calendar.AddAsync(recurrentEvent);
        await DbContext.SaveChangesAsync();
        var occurrence = (await Calendar.OccurrencesAsync(JAN1_2023_UTC, JAN1_2023_UTC.AddDays(1)))
            .Single(x => OccurrenceId.Parse(x.Id).EventId == recurrentEvent.Id);
        await Calendar.MoveAsync(occurrence.Id, occurrence.Period.Move(TimeSpan.FromHours(1)));
        await DbContext.SaveChangesAsync();

        var result = await Repository.GetAllAsync(EventEntityType.OneTimeEvent);

        result.Should().ContainSingle()
            .Which.Should().BeOfType<Event<EventData>>()
            .Which.Id.Should().Be(oneTimeEvent.Id);
    }

    [Test]
    public async Task OccurrenceAdjustment_ShouldNotReturnOneTimeEvents()
    {
        var oneTimeEvent = Event<EventData>.New(
            Period.New(JAN1_2023_UTC.AddHours(5), JAN1_2023_UTC.AddHours(6)),
            TimeZoneInfo.Utc.Id,
            EventData.Test());
        var recurrentEvent = Event.Factory.MGWeekly(
            Period.New(JAN1_2023_UTC.AddHours(6), JAN1_2023_UTC.AddHours(7)),
            TimeZoneInfo.Utc.Id,
            EventData.Test(),
            [DayOfWeek.Sunday]);

        await Calendar.AddAsync(oneTimeEvent);
        await Calendar.AddAsync(recurrentEvent);
        await DbContext.SaveChangesAsync();
        var occurrence = (await Calendar.OccurrencesAsync(JAN1_2023_UTC, JAN1_2023_UTC.AddDays(1)))
            .Single(x => OccurrenceId.Parse(x.Id).EventId == recurrentEvent.Id);
        await Calendar.MoveAsync(occurrence.Id, occurrence.Period.Move(TimeSpan.FromHours(1)));
        await DbContext.SaveChangesAsync();

        var result = await Repository.GetAllAsync(EventEntityType.OccurrenceAdjustment);

        result.Should().ContainSingle()
            .Which.Should().BeOfType<OccurrenceAdjustment<EventData>>();
    }

    private IEventRepository<EventData> Repository => Services.GetRequiredService<IEventRepository<EventData>>();

    [SetUp]
    public void SetUp()
    {
        CleanDatabase();
    }
}
