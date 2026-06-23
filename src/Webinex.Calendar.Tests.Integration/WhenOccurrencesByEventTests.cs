using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Webinex.Calendar.Extensions;
using Webinex.Calendar.Services;
using Webinex.Calendar.Tests.Integration.Setups;

namespace Webinex.Calendar.Tests.Integration;

public class WhenOccurrencesByEventTests : IntegrationTestsBase
{
    [Test]
    public async Task CountOnlyForEndlessRecurrentEvent_ShouldReturnLimitedOccurrences()
    {
        var @event = Event.Factory.MGWeekly(
            Period.New(JAN1_2023_UTC.AddHours(6), JAN1_2023_UTC.AddHours(7)),
            TimeZoneInfo.Utc.Id,
            EventData.Test(),
            [DayOfWeek.Sunday]);

        await Calendar.AddAsync(@event);
        await DbContext.SaveChangesAsync();

        var result = await OccurrenceReadService.OccurrencesByEventAsync(
            new OccurrencesByEventQueryArgs([@event.Id], count: 2));

        result[@event.Id].Select(x => x.Period).Should().Equal(
            Period.New(JAN1_2023_UTC.AddHours(6), JAN1_2023_UTC.AddHours(7)),
            Period.New(JAN1_2023_UTC.AddDays(7).AddHours(6), JAN1_2023_UTC.AddDays(7).AddHours(7)));
    }

    [Test]
    public async Task PeriodAndCount_WhenEventsRequestedOutOfOrder_ShouldReturnOccurrencesGroupedByEvent()
    {
        var sunday = Event.Factory.MGWeekly(
            Period.New(JAN1_2023_UTC.AddHours(6), JAN1_2023_UTC.AddHours(7)),
            TimeZoneInfo.Utc.Id,
            EventData.Test(),
            [DayOfWeek.Sunday]);
        var monday = Event.Factory.MGWeekly(
            Period.New(JAN1_2023_UTC.AddHours(8), JAN1_2023_UTC.AddHours(9)),
            TimeZoneInfo.Utc.Id,
            EventData.Test(),
            [DayOfWeek.Monday]);

        await Calendar.AddAsync(sunday);
        await Calendar.AddAsync(monday);
        await DbContext.SaveChangesAsync();

        var result = await OccurrenceReadService.OccurrencesByEventAsync(
            new OccurrencesByEventQueryArgs(
                [monday.Id, sunday.Id],
                count: 1,
                period: new OpenPeriod<DateTimeOffset>(JAN1_2023_UTC, JAN1_2023_UTC.AddDays(3))));

        result[sunday.Id].Should().ContainSingle()
            .Which.Period.Should().BeEquivalentTo(
                Period.New(JAN1_2023_UTC.AddHours(6), JAN1_2023_UTC.AddHours(7)));
        result[monday.Id].Should().ContainSingle()
            .Which.Period.Should().BeEquivalentTo(
                Period.New(JAN1_2023_UTC.AddDays(1).AddHours(8), JAN1_2023_UTC.AddDays(1).AddHours(9)));
    }

    [Test]
    public async Task RespectAdjustments_WhenOccurrenceMovedEarlier_ShouldReturnMovedOccurrenceFirst()
    {
        var @event = Event.Factory.MGWeekly(
            Period.New(JAN1_2023_UTC.AddHours(6), JAN1_2023_UTC.AddHours(7)),
            TimeZoneInfo.Utc.Id,
            EventData.Test(),
            [DayOfWeek.Sunday]);

        await Calendar.AddAsync(@event);
        await DbContext.SaveChangesAsync();

        var original = Period.New(JAN1_2023_UTC.AddDays(7).AddHours(6), JAN1_2023_UTC.AddDays(7).AddHours(7));
        var moved = Period.New(JAN1_2023_UTC.AddHours(5), JAN1_2023_UTC.AddHours(6));
        await Calendar.MoveAsync(new OccurrenceId(@event.Id, original.Start).ToString(), moved);
        await DbContext.SaveChangesAsync();

        var result = await OccurrenceReadService.OccurrencesByEventAsync(
            new OccurrencesByEventQueryArgs(
                [@event.Id],
                count: 1,
                period: new OpenPeriod<DateTimeOffset>(JAN1_2023_UTC, JAN1_2023_UTC.AddDays(8)),
                isRespectAdjustments: true));

        result[@event.Id].Should().ContainSingle()
            .Which.Period.Should().BeEquivalentTo(moved);
    }

    [Test]
    public async Task IgnoreAdjustments_WhenOccurrenceMovedEarlier_ShouldReturnOriginalOccurrence()
    {
        var @event = Event.Factory.MGWeekly(
            Period.New(JAN1_2023_UTC.AddHours(6), JAN1_2023_UTC.AddHours(7)),
            TimeZoneInfo.Utc.Id,
            EventData.Test(),
            [DayOfWeek.Sunday]);

        await Calendar.AddAsync(@event);
        await DbContext.SaveChangesAsync();

        var original = Period.New(JAN1_2023_UTC.AddDays(7).AddHours(6), JAN1_2023_UTC.AddDays(7).AddHours(7));
        var moved = Period.New(JAN1_2023_UTC.AddHours(5), JAN1_2023_UTC.AddHours(6));
        await Calendar.MoveAsync(new OccurrenceId(@event.Id, original.Start).ToString(), moved);
        await DbContext.SaveChangesAsync();

        var result = await OccurrenceReadService.OccurrencesByEventAsync(
            new OccurrencesByEventQueryArgs(
                [@event.Id],
                count: 2,
                period: new OpenPeriod<DateTimeOffset>(JAN1_2023_UTC, JAN1_2023_UTC.AddDays(8)),
                isRespectAdjustments: false));

        result[@event.Id].Select(x => x.Period).Should().Equal(
            Period.New(JAN1_2023_UTC.AddHours(6), JAN1_2023_UTC.AddHours(7)),
            original);
    }

    private IOccurrenceReadService<EventData> OccurrenceReadService =>
        Services.GetRequiredService<IOccurrenceReadService<EventData>>();

    [SetUp]
    public void SetUp()
    {
        CleanDatabase();
    }
}
