using FluentAssertions;
using Webinex.Calendar.Common;
using Webinex.Calendar.Events;
using Webinex.Calendar.Tests.Integration.Common;
using Webinex.Calendar.Tests.Integration.Setups;

namespace Webinex.Calendar.Tests.Integration;

public class WhenMoveRecurrentEventTests : IntegrationTestsBase
{
    [Test]
    public async Task WhenMoveAndBothMatch_ShouldBeMoved()
    {
        var @event = RecurrentEvent<EventData>.NewInterval(
            JAN1_2023_UTC,
            null,
            TimeSpan.FromHours(12).TotalMinutes.Round(),
            TimeSpan.FromHours(1).TotalMinutes.Round(),
            new EventData("NAME"));

        await Calendar.AddRecurrentEventAsync(@event);
        await DbContext.SaveChangesAsync();

        var eventsBefore = await Calendar.GetAllAsync(JAN1_2023_UTC, JAN1_2023_UTC.AddDays(1));
        eventsBefore = eventsBefore.OrderBy(x => x.Start).ToArray();

        eventsBefore.Length.Should().Be(2);
        
        eventsBefore[0].Start.Should().Be(JAN1_2023_UTC);
        eventsBefore[0].End.Should().Be(JAN1_2023_UTC.AddHours(1));
        
        eventsBefore[1].Start.Should().Be(JAN1_2023_UTC.AddHours(12));
        eventsBefore[1].End.Should().Be(JAN1_2023_UTC.AddHours(13));

        await Calendar.MoveRecurrentEventAsync(@event, JAN1_2023_UTC,
            new Period(JAN1_2023_UTC.AddHours(3), JAN1_2023_UTC.AddHours(5)));


        await DbContext.SaveChangesAsync();
        var eventsAfter = await Calendar.GetAllAsync(JAN1_2023_UTC, JAN1_2023_UTC.AddDays(1));

        eventsAfter.Length.Should().Be(2);
        
        eventsAfter[0].Start.Should().Be(JAN1_2023_UTC.AddHours(3));
        eventsAfter[0].End.Should().Be(JAN1_2023_UTC.AddHours(5));
        
        eventsAfter[1].Start.Should().Be(JAN1_2023_UTC.AddHours(12));
        eventsAfter[1].End.Should().Be(JAN1_2023_UTC.AddHours(13));
    }

    [Test]
    public async Task WhenMoveAndMovedToDoesntMatch_ShouldNotContainOriginal()
    {
        var @event = RecurrentEvent<EventData>.NewInterval(
            JAN1_2023_UTC,
            null,
            TimeSpan.FromHours(12).TotalMinutes.Round(),
            TimeSpan.FromHours(1).TotalMinutes.Round(),
            new EventData("NAME"));

        await Calendar.AddRecurrentEventAsync(@event);
        await DbContext.SaveChangesAsync();

        var eventsBefore = await Calendar.GetAllAsync(JAN1_2023_UTC, JAN1_2023_UTC.AddDays(1));
        eventsBefore = eventsBefore.OrderBy(x => x.Start).ToArray();

        eventsBefore.Length.Should().Be(2);
        
        eventsBefore[0].Start.Should().Be(JAN1_2023_UTC);
        eventsBefore[0].End.Should().Be(JAN1_2023_UTC.AddHours(1));
        
        eventsBefore[1].Start.Should().Be(JAN1_2023_UTC.AddHours(12));
        eventsBefore[1].End.Should().Be(JAN1_2023_UTC.AddHours(13));

        await Calendar.MoveRecurrentEventAsync(@event, JAN1_2023_UTC,
            new Period(JAN1_2023_UTC.AddDays(1), JAN1_2023_UTC.AddDays(1).AddHours(1)));

        await DbContext.SaveChangesAsync();
        var eventsAfter = await Calendar.GetAllAsync(JAN1_2023_UTC, JAN1_2023_UTC.AddDays(1));

        eventsAfter.Length.Should().Be(1);

        eventsAfter[0].Start.Should().Be(JAN1_2023_UTC.AddHours(12));
        eventsAfter[0].End.Should().Be(JAN1_2023_UTC.AddHours(13));
    }

    [Test]
    public async Task WhenMoveAndMatchMovedToButOriginalDoesntMatch_ShouldExists()
    {
        var @event = RecurrentEvent<EventData>.NewInterval(
            JAN1_2023_UTC,
            null,
            TimeSpan.FromDays(7).TotalMinutes.Round(),
            TimeSpan.FromHours(1).TotalMinutes.Round(),
            new EventData("NAME"));

        await Calendar.AddRecurrentEventAsync(@event);
        await DbContext.SaveChangesAsync();

        var eventsBefore = await Calendar.GetAllAsync(JAN1_2023_UTC.AddDays(1), JAN1_2023_UTC.AddDays(2));
        eventsBefore.Length.Should().Be(0);

        await Calendar.MoveRecurrentEventAsync(@event, JAN1_2023_UTC,
            new Period(JAN1_2023_UTC.AddDays(1), JAN1_2023_UTC.AddDays(1).AddHours(1)));

        await DbContext.SaveChangesAsync();
        var eventsAfter = await Calendar.GetAllAsync(JAN1_2023_UTC.AddDays(1), JAN1_2023_UTC.AddDays(2));

        eventsAfter.Length.Should().Be(1);

        eventsAfter[0].Start.Should().Be(JAN1_2023_UTC.AddDays(1));
        eventsAfter[0].End.Should().Be(JAN1_2023_UTC.AddDays(1).AddHours(1));
    }

    [SetUp]
    public new void SetUp()
    {
        CleanDatabase();
    }
}