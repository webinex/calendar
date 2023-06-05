using FluentAssertions;
using Webinex.Asky;
using Webinex.Calendar.Common;
using Webinex.Calendar.Events;
using Webinex.Calendar.Tests.Integration.Setups;

namespace Webinex.Calendar.Tests.Integration;

public class WhenGetAllStatesWithFiltersTests : IntegrationTestsBase
{
    [Test]
    public async Task WithAllFilters_ShouldReturnCorrectResult()
    {
        var @event = RecurrentEvent<EventData>.NewWeekday(
            JAN1_2023_UTC,
            null,
            (int)TimeSpan.FromHours(6).TotalMinutes,
            (int)TimeSpan.FromHours(1).TotalMinutes,
            new[] { Weekday.Sunday, Weekday.Tuesday },
            new EventData("NAME"));

        await Calendar.Recurrent.AddAsync(@event);
        await DbContext.SaveChangesAsync();

        var from = JAN1_2023_UTC;
        var to = JAN1_2023_UTC.Add(TimeSpan.FromDays(7));
        var events = await Calendar.GetCalculatedAsync(from, to);

        var firstAppearance = events.First();
        var secondAppearance = events.Skip(1).First();
        var moveFirstToPeriod = new Period(firstAppearance.Start.AddHours(1), firstAppearance.End.AddHours(1));
        var moveSecondToPeriod = new Period(secondAppearance.Start.AddHours(1), secondAppearance.End.AddHours(1));

        await Calendar.Recurrent.MoveAsync(@event, firstAppearance.Start, moveFirstToPeriod);
        await Calendar.Recurrent.MoveAsync(@event, secondAppearance.Start, moveSecondToPeriod);
        await DbContext.SaveChangesAsync();

        var filters = FilterRule.And(
            FilterRule.Eq("recurrentEventId", @event.Id),
            FilterRule.Eq("period.start", firstAppearance.Start),
            FilterRule.Eq("period.end", firstAppearance.End),
            FilterRule.Eq("moveTo.start", moveFirstToPeriod.Start),
            FilterRule.Eq("moveTo.end", moveFirstToPeriod.End)
            );
        var states = await Calendar.Recurrent.GetAllStatesAsync(filters);

        states.Length.Should().Be(1);
    }
    
    [Test]
    public async Task FilterOnlyByRecurrentEventId_ShouldReturnCorrectResult()
    {
        var @event = RecurrentEvent<EventData>.NewWeekday(
            JAN1_2023_UTC,
            null,
            (int)TimeSpan.FromHours(6).TotalMinutes,
            (int)TimeSpan.FromHours(1).TotalMinutes,
            new[] { Weekday.Sunday, Weekday.Tuesday },
            new EventData("NAME"));

        await Calendar.Recurrent.AddAsync(@event);
        await DbContext.SaveChangesAsync();

        var from = JAN1_2023_UTC;
        var to = JAN1_2023_UTC.Add(TimeSpan.FromDays(7));
        var events = await Calendar.GetCalculatedAsync(from, to);

        var firstAppearance = events.First();
        var secondAppearance = events.Skip(1).First();
        var moveFirstToPeriod = new Period(firstAppearance.Start.AddHours(1), firstAppearance.End.AddHours(1));
        var moveSecondToPeriod = new Period(secondAppearance.Start.AddHours(1), secondAppearance.End.AddHours(1));

        await Calendar.Recurrent.MoveAsync(@event, firstAppearance.Start, moveFirstToPeriod);
        await Calendar.Recurrent.MoveAsync(@event, secondAppearance.Start, moveSecondToPeriod);
        await DbContext.SaveChangesAsync();

        var filters = FilterRule.And(
            FilterRule.Eq("recurrentEventId", @event.Id),
            FilterRule.Eq("recurrentEventId", @event.Id)
        );
        var states = await Calendar.Recurrent.GetAllStatesAsync(filters);

        states.Length.Should().Be(2);
    }
    
    [SetUp]
    public new void SetUp()
    {
        CleanDatabase();
    }
}