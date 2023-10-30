using FluentAssertions;
using Webinex.Calendar.Common;
using Webinex.Calendar.Events;
using Webinex.Calendar.Tests.Integration.Setups;

namespace Webinex.Calendar.Tests.Integration;

public class WhenGetManyStatesTests : IntegrationTestsBase
{
    [Test]
    public async Task MoveAppearance_ShouldReturnCorrectResult()
    {
        var @event = RecurrentEvent<EventData>.NewWeekday(
            JAN1_2023_UTC,
            null,
            (int)TimeSpan.FromHours(6).TotalMinutes,
            (int)TimeSpan.FromHours(1).TotalMinutes,
            new[] { Weekday.Sunday, Weekday.Tuesday },
            TimeZoneInfo.Utc,
            new EventData("NAME"));

        await Calendar.Recurrent.AddAsync(@event);
        await DbContext.SaveChangesAsync();

        var from = JAN1_2023_UTC;
        var to = JAN1_2023_UTC.Add(TimeSpan.FromDays(7));
        var events = await Calendar.GetCalculatedAsync(from, to);

        var firstAppearance = events.First();
        var moveFirstToPeriod = new Period(firstAppearance.Start.AddHours(1), firstAppearance.End.AddHours(1));

        await Calendar.Recurrent.MoveAsync(@event, firstAppearance.Start, moveFirstToPeriod);
        await DbContext.SaveChangesAsync();
        DbContext.ChangeTracker.Clear();
        
        var states = await Calendar.Recurrent.GetManyStatesAsync(new []
        {
            new RecurrentEventStateId(@event.Id, firstAppearance.Start),
        });

        states.Length.Should().Be(1);
    }

    [SetUp]
    public new void SetUp()
    {
        CleanDatabase();
    }
}