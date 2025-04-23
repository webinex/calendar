using FluentAssertions;
using Webinex.Asky;
using Webinex.Calendar.Extensions;
using Webinex.Calendar.Tests.Integration.Setups;

namespace Webinex.Calendar.Tests.Integration;

public class WhenGetAllStatesWithFiltersTests : IntegrationTestsBase
{
    [Test]
    public async Task WithAllFilters_ShouldReturnCorrectResult()
    {
        var @event = Event.Factory.MGWeekly(
            Period.New(JAN1_2023_UTC.AddHours(6), JAN1_2023_UTC.AddHours(6).AddHours(1)),
            TimeZoneInfo.Utc.Id,
            EventData.Test(),
            [DayOfWeek.Sunday, DayOfWeek.Tuesday]);

        await Calendar.AddAsync(@event);
        await DbContext.SaveChangesAsync();

        var from = JAN1_2023_UTC;
        var to = JAN1_2023_UTC.Add(TimeSpan.FromDays(7));
        var events = await Calendar.OccurrencesAsync(from, to);

        var firstAppearance = events.First();
        var secondAppearance = events.Skip(1).First();
        var moveFirstToPeriod = new Period<DateTimeOffset>(firstAppearance.Period.Start.AddHours(1),
            firstAppearance.Period.End.AddHours(1));
        var moveSecondToPeriod = new Period<DateTimeOffset>(secondAppearance.Period.Start.AddHours(1),
            secondAppearance.Period.End.AddHours(1));

        await Calendar.MoveAsync(firstAppearance.Id, moveFirstToPeriod);
        await Calendar.MoveAsync(secondAppearance.Id, moveSecondToPeriod);
        await DbContext.SaveChangesAsync();

        var filters = FilterRule.And(
            FilterRule.Eq("recurrentEventId", @event.Id),
            FilterRule.Eq("period.start", firstAppearance.Period.Start),
            FilterRule.Eq("period.end", firstAppearance.Period.End),
            FilterRule.Eq("moveTo.start", moveFirstToPeriod.Start),
            FilterRule.Eq("moveTo.end", moveFirstToPeriod.End)
        );

        var states = await Calendar.GetAllAsync<OccurrenceAdjustment<EventData>>(filters);

        states.Count.Should().Be(1);
    }

    [Test]
    public async Task FilterOnlyByRecurrentEventId_ShouldReturnCorrectResult()
    {
        var @event = Event.Factory.MGWeekly(
            Period.New(JAN1_2023_UTC.AddHours(6), JAN1_2023_UTC.AddHours(6).AddHours(1)),
            TimeZoneInfo.Utc.Id,
            EventData.Test(),
            [DayOfWeek.Sunday, DayOfWeek.Tuesday]);

        await Calendar.AddAsync(@event);
        await DbContext.SaveChangesAsync();

        var from = JAN1_2023_UTC;
        var to = JAN1_2023_UTC.Add(TimeSpan.FromDays(7));
        var events = await Calendar.OccurrencesAsync(from, to);

        var firstAppearance = events.First();
        var secondAppearance = events.Skip(1).First();
        var moveFirstToPeriod =
            Period.New(firstAppearance.Period.Start.AddHours(1), firstAppearance.Period.End.AddHours(1));
        var moveSecondToPeriod = Period.New(secondAppearance.Period.Start.AddHours(1),
            secondAppearance.Period.End.AddHours(1));

        await Calendar.MoveAsync(firstAppearance.Id, moveFirstToPeriod);
        await Calendar.MoveAsync(secondAppearance.Id, moveSecondToPeriod);
        await DbContext.SaveChangesAsync();

        var filters = FilterRule.Eq("recurrentEventId", @event.Id);
        var states = await Calendar.GetAllAsync<OccurrenceAdjustment<EventData>>(filters);

        states.Count.Should().Be(2);
    }

    [SetUp]
    public void SetUp()
    {
        CleanDatabase();
    }
}