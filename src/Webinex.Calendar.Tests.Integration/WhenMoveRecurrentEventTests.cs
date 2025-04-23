using FluentAssertions;
using Webinex.Asky;
using Webinex.Calendar.Extensions;
using Webinex.Calendar.Tests.Integration.Setups;

namespace Webinex.Calendar.Tests.Integration;

public class WhenMoveRecurrentEventTests : IntegrationTestsBase
{
    [Test]
    public async Task WhenMoveAndBothMatch_ShouldBeMoved()
    {
        var @event = Event.Factory.MGDaily(
            Period.New(JAN1_2023_UTC, JAN1_2023_UTC.AddHours(1)),
            TimeZoneInfo.Utc.Id,
            EventData.Test());

        await Calendar.AddAsync(@event);
        await DbContext.SaveChangesAsync();

        var eventsBefore = await Calendar.OccurrencesAsync(JAN1_2023_UTC, JAN1_2023_UTC.AddDays(2));

        eventsBefore.Count.Should().Be(2);

        eventsBefore.ElementAt(0).Period.Start.Should().Be(JAN1_2023_UTC);
        eventsBefore.ElementAt(0).Period.End.Should().Be(JAN1_2023_UTC.AddHours(1));

        eventsBefore.ElementAt(1).Period.Start.Should().Be(JAN1_2023_UTC.AddDays(1));
        eventsBefore.ElementAt(1).Period.End.Should().Be(JAN1_2023_UTC.AddDays(1).AddHours(1));

        await Calendar.MoveAsync(
            eventsBefore.ElementAt(0).Id,
            Period.New(JAN1_2023_UTC.AddHours(3), JAN1_2023_UTC.AddHours(5)));

        await DbContext.SaveChangesAsync();
        var eventsAfter = await Calendar.OccurrencesAsync(JAN1_2023_UTC, JAN1_2023_UTC.AddDays(2));

        eventsAfter.Count.Should().Be(2);

        eventsAfter.ElementAt(0).Period.Start.Should().Be(JAN1_2023_UTC.AddHours(3));
        eventsAfter.ElementAt(0).Period.End.Should().Be(JAN1_2023_UTC.AddHours(5));

        eventsAfter.ElementAt(1).Period.Start.Should().Be(JAN1_2023_UTC.AddDays(1));
        eventsAfter.ElementAt(1).Period.End.Should().Be(JAN1_2023_UTC.AddDays(1).AddHours(1));
    }

    [Test]
    public async Task WhenMoveAndMovedToDoesntMatch_ShouldNotContainOriginal()
    {
        var @event = Event.Factory.MGDaily(
            Period.New(JAN1_2023_UTC, JAN1_2023_UTC.AddHours(1)),
            TimeZoneInfo.Utc.Id,
            EventData.Test());

        await Calendar.AddAsync(@event);
        await DbContext.SaveChangesAsync();

        var eventsBefore = await Calendar.OccurrencesAsync(JAN1_2023_UTC, JAN1_2023_UTC.AddDays(2));
        eventsBefore = eventsBefore.OrderBy(x => x.Period.Start).ToArray();

        eventsBefore.Count.Should().Be(2);

        eventsBefore.ElementAt(0).Period.Start.Should().Be(JAN1_2023_UTC);
        eventsBefore.ElementAt(0).Period.End.Should().Be(JAN1_2023_UTC.AddHours(1));

        eventsBefore.ElementAt(1).Period.Start.Should().Be(JAN1_2023_UTC.AddDays(1));
        eventsBefore.ElementAt(1).Period.End.Should().Be(JAN1_2023_UTC.AddDays(1).AddHours(1));

        await Calendar.MoveAsync(
            eventsBefore.ElementAt(0).Id,
            eventsBefore.ElementAt(0).Period.Move(TimeSpan.FromDays(2)));

        await DbContext.SaveChangesAsync();
        var eventsAfter = await Calendar.OccurrencesAsync(JAN1_2023_UTC, JAN1_2023_UTC.AddDays(2));

        eventsAfter.Count.Should().Be(1);

        eventsAfter.ElementAt(0).Period.Start.Should().Be(JAN1_2023_UTC.AddDays(1));
        eventsAfter.ElementAt(0).Period.End.Should().Be(JAN1_2023_UTC.AddDays(1).AddHours(1));
    }

    [Test]
    public async Task WhenMoveAndMatchMovedToButOriginalDoesntMatch_ShouldExists()
    {
        var @event = Event.Factory.MGDaily(
            Period.New(JAN1_2023_UTC, JAN1_2023_UTC.AddHours(1)),
            TimeZoneInfo.Utc.Id,
            EventData.Test(),
            interval: 7);

        await Calendar.AddAsync(@event);
        await DbContext.SaveChangesAsync();

        var eventsBefore = await Calendar.OccurrencesAsync(JAN1_2023_UTC.AddDays(1), JAN1_2023_UTC.AddDays(2));
        eventsBefore.Count.Should().Be(0);

        await Calendar.MoveAsync(
            new OccurrenceId(@event.Id, JAN1_2023_UTC).ToString(),
            Period.New(JAN1_2023_UTC.AddDays(1), JAN1_2023_UTC.AddDays(1).AddHours(1)));

        await DbContext.SaveChangesAsync();
        var eventsAfter = await Calendar.OccurrencesAsync(JAN1_2023_UTC.AddDays(1), JAN1_2023_UTC.AddDays(2));

        eventsAfter.Count.Should().Be(1);

        eventsAfter.ElementAt(0).Period.Start.Should().Be(JAN1_2023_UTC.AddDays(1));
        eventsAfter.ElementAt(0).Period.End.Should().Be(JAN1_2023_UTC.AddDays(1).AddHours(1));
    }

    [Test]
    public async Task
        WhenMoveOutsideOfRecurrentEventEffectivePeriod_SaveNewData_FilteredByOldDataInNewPeriod_VisitShouldNotBeInResult()
    {
        // Arrange
        var @event = Event.Factory.MGWeekly(
            Period.New(
                JAN1_2023_UTC.AddHours(10),
                JAN1_2023_UTC.AddHours(10).AddHours(2)),
            TimeZoneInfo.Utc.Id,
            new EventData("NAME_1"),
            [DayOfWeek.Sunday]);

        var movedVisitOriginalStart = JAN1_2023_UTC.AddDays(7).AddHours(10);
        var movedVisitNewStart = JAN1_2023_UTC.AddDays(13).AddHours(10);

        await Calendar.AddAsync(@event);
        await DbContext.SaveChangesAsync();

        // Act
        var occurrenceId = new OccurrenceId(@event.Id, movedVisitOriginalStart).ToString();

        await Calendar.MoveAsync(
            occurrenceId,
            Period.New(movedVisitNewStart, movedVisitNewStart.AddHours(2)));

        await Calendar.SaveDataAsync(occurrenceId, new EventData("NAME_2"));
        await DbContext.SaveChangesAsync();
        DbContext.ChangeTracker.Clear();

        var searchedVisitResult = await Calendar.OccurrencesAsync(
            JAN1_2023_UTC.AddDays(13),
            JAN1_2023_UTC.AddDays(14),
            FilterRule.Eq("name", "NAME_1"));

        // Assert
        searchedVisitResult.Count.Should().Be(0);
    }

    [SetUp]
    public void SetUp()
    {
        CleanDatabase();
    }
}