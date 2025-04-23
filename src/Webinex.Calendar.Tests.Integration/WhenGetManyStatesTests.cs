using FluentAssertions;
using Webinex.Calendar.Extensions;
using Webinex.Calendar.Tests.Integration.Setups;

namespace Webinex.Calendar.Tests.Integration;

public class WhenGetManyStatesTests : IntegrationTestsBase
{
    [Test]
    public async Task MoveAppearance_ShouldReturnCorrectResult()
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
        var moveFirstToPeriod = firstAppearance.Period.Move(TimeSpan.FromHours(1));

        await Calendar.MoveAsync(firstAppearance.Id, moveFirstToPeriod);
        await DbContext.SaveChangesAsync();
        DbContext.ChangeTracker.Clear();
        
        var state = await Calendar.ByIdAsync<OccurrenceAdjustment<EventData>>(firstAppearance.Id);

        state.Should().NotBeNull();
        state!.MoveTo!.Start.Should().Be(moveFirstToPeriod.Start);
        state.MoveTo!.End.Should().Be(moveFirstToPeriod.End);
    }

    [SetUp]
    public void SetUp()
    {
        CleanDatabase();
    }
}