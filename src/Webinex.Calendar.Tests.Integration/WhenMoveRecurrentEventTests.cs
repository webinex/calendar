using FluentAssertions;
using Webinex.Asky;
using Webinex.Calendar.Extensions;
using Webinex.Calendar.MicrosoftGraph;
using Webinex.Calendar.Tests.Integration.Common;
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
    public async Task WhenMoveWeeklyOccurrenceForwardAndBack_ShouldReturnToOriginalTime()
    {
        var start = DateTimeOffset.Parse("2026-06-22T13:00:00+000"); // Monday
        var monday = start;
        var tuesday = start.AddDays(1);
        var wednesday = start.AddDays(2);
        var searchEnd = start.AddDays(3);

        var @event = Event.Factory.MGWeekly(
            Period.New(start, start.AddHours(1)),
            TimeZoneInfo.Utc.Id,
            EventData.Test(),
            [DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday]);

        await Calendar.AddAsync(@event);
        await DbContext.SaveChangesAsync();

        var eventsBefore = (await Calendar.OccurrencesAsync(start, searchEnd))
            .OrderBy(x => x.Period.Start)
            .ToArray();
        var tuesdayOccurrence = eventsBefore.Single(x => x.Period.Start == tuesday);

        await Calendar.MoveAsync(
            tuesdayOccurrence.Id,
            Period.New(tuesday.AddHours(2), tuesday.AddHours(3)));
        await DbContext.SaveChangesAsync();

        var eventsAfterMoveForward = (await Calendar.OccurrencesAsync(start, searchEnd))
            .OrderBy(x => x.Period.Start)
            .ToArray();

        eventsAfterMoveForward.Select(x => x.Period).Should().Equal(
            Period.New(monday, monday.AddHours(1)),
            Period.New(tuesday.AddHours(2), tuesday.AddHours(3)),
            Period.New(wednesday, wednesday.AddHours(1)));

        await Calendar.MoveAsync(
            tuesdayOccurrence.Id,
            Period.New(tuesday, tuesday.AddHours(1)));
        await DbContext.SaveChangesAsync();

        var eventsAfterMoveBack = (await Calendar.OccurrencesAsync(start, searchEnd))
            .OrderBy(x => x.Period.Start)
            .ToArray();

        eventsAfterMoveBack.Select(x => x.Period).Should().Equal(
            Period.New(monday, monday.AddHours(1)),
            Period.New(tuesday, tuesday.AddHours(1)),
            Period.New(wednesday, wednesday.AddHours(1)));

        var occurrenceAdjustment = await Calendar.ByIdAsync<OccurrenceAdjustment<EventData>>(tuesdayOccurrence.Id);
        occurrenceAdjustment.Should().BeNull();
    }

    [Test]
    public async Task WhenMoveThirdDailyPrevDayOffsetOccurrenceAsGroup_ShouldMoveFollowingOccurrences()
    {
        var firstOccurrence = DateTimeOffset.Parse("2026-06-20T18:00:00+00:00"); // 21 Jun 2026 08:00 Kiritimati

        var @event = Event.Factory.MGDaily(
            Period.New(firstOccurrence, firstOccurrence.AddHours(1)),
            "Pacific/Kiritimati",
            EventData.Test());

        await Calendar.AddAsync(@event);
        await DbContext.SaveChangesAsync();

        var searchPeriod = Period.New(
            DateTimeOffset.Parse("2026-06-20T17:00:00+00:00"),
            DateTimeOffset.Parse("2026-06-24T21:00:00+00:00"));

        var eventsBefore = (await Calendar.OccurrencesAsync(searchPeriod))
            .OrderBy(x => x.Period.Start)
            .ToArray();
        var thirdOccurrence = eventsBefore.ElementAt(2);

        thirdOccurrence.Period.Should().Be(Period.New(
            DateTimeOffset.Parse("2026-06-22T18:00:00+00:00"),
            DateTimeOffset.Parse("2026-06-22T19:00:00+00:00")));

        await Calendar.UpdateOccurenceAsync(new UpdateOccurrenceArgs<EventData>(
            thirdOccurrence.Id,
            period: new Optional<Period<DateTimeOffset>>(Period.New(
                DateTimeOffset.Parse("2026-06-22T19:00:00+00:00"),
                DateTimeOffset.Parse("2026-06-22T20:00:00+00:00"))),
            recurrence: new Optional<Recurrence>(@event.Recurrence!.WithPeriod(DateOnly.Parse("2026-06-23"), null)),
            behavior: OccurenceUpdateBehavior.Group));
        await DbContext.SaveChangesAsync();

        var eventsAfter = (await Calendar.OccurrencesAsync(searchPeriod))
            .OrderBy(x => x.Period.Start)
            .ToArray();

        eventsAfter.Select(x => x.Period).Should().Equal(
            Period.New(DateTimeOffset.Parse("2026-06-20T18:00:00+00:00"), DateTimeOffset.Parse("2026-06-20T19:00:00+00:00")),
            Period.New(DateTimeOffset.Parse("2026-06-21T18:00:00+00:00"), DateTimeOffset.Parse("2026-06-21T19:00:00+00:00")),
            Period.New(DateTimeOffset.Parse("2026-06-22T19:00:00+00:00"), DateTimeOffset.Parse("2026-06-22T20:00:00+00:00")),
            Period.New(DateTimeOffset.Parse("2026-06-23T19:00:00+00:00"), DateTimeOffset.Parse("2026-06-23T20:00:00+00:00")),
            Period.New(DateTimeOffset.Parse("2026-06-24T19:00:00+00:00"), DateTimeOffset.Parse("2026-06-24T20:00:00+00:00")));

        var recurrentEvents = await Calendar.GetAllAsync<Event<EventData>>(null, null, null);
        recurrentEvents.Should().ContainSingle(x =>
            x.Period.Start == DateTimeOffset.Parse("2026-06-22T19:00:00+00:00") &&
            x.Recurrence!.MGRecurrence!.Period.Start == DateOnly.Parse("2026-06-23"));
    }

    [Test]
    public async Task WhenMoveFirstActualNegaviteOffsetToPrevDayOccurrenceAsGroup_ShouldNotEndParentBeforeItsFirstOccurrence()
    {
        var period = Period.New(
            DateTimeOffset.Parse("2026-06-22T18:00:00+00:00"),
            DateTimeOffset.Parse("2026-06-22T19:00:00+00:00"));
        var recurrence = new Recurrence(
            new MGRecurrence(
                new MGRecurrencePeriod(DateOnly.Parse("2026-06-22"), DateOnly.Parse("2026-06-23")),
                MGRecurrencePattern.Weekly([
                    DayOfWeek.Monday,
                    DayOfWeek.Tuesday,
                    DayOfWeek.Wednesday,
                    DayOfWeek.Thursday,
                    DayOfWeek.Friday,
                    DayOfWeek.Saturday,
                    DayOfWeek.Sunday,
                ])));
        var @event = Event.New(period, "Pacific/Kiritimati", EventData.Test(), recurrence);

        await Calendar.AddAsync(@event);
        await DbContext.SaveChangesAsync();

        var occurrenceId = new OccurrenceId(@event.Id, DateTimeOffset.Parse("2026-06-22T18:00:00+00:00")).ToString();

        await Calendar.UpdateOccurenceAsync(new UpdateOccurrenceArgs<EventData>(
            occurrenceId,
            period: new Optional<Period<DateTimeOffset>>(Period.New(
                DateTimeOffset.Parse("2026-06-22T19:00:00+00:00"),
                DateTimeOffset.Parse("2026-06-22T20:00:00+00:00"))),
            timeZone: new Optional<string>("Pacific/Kiritimati"),
            recurrence: new Optional<Recurrence>(recurrence.WithPeriod(DateOnly.Parse("2026-06-23"), null)),
            behavior: OccurenceUpdateBehavior.Group));
        await DbContext.SaveChangesAsync();

        var result = (await Calendar.OccurrencesAsync(
                DateTimeOffset.Parse("2026-06-22T00:00:00+00:00"),
                DateTimeOffset.Parse("2026-06-24T21:00:00+00:00")))
            .OrderBy(x => x.Period.Start)
            .ToArray();

        result.Select(x => x.Period).Should().Equal(
            Period.New(DateTimeOffset.Parse("2026-06-22T19:00:00+00:00"), DateTimeOffset.Parse("2026-06-22T20:00:00+00:00")),
            Period.New(DateTimeOffset.Parse("2026-06-23T19:00:00+00:00"), DateTimeOffset.Parse("2026-06-23T20:00:00+00:00")),
            Period.New(DateTimeOffset.Parse("2026-06-24T19:00:00+00:00"), DateTimeOffset.Parse("2026-06-24T20:00:00+00:00")));
    }

    [Test]
    public async Task WhenMoveExistingOccurrenceAdjustmentToOriginalPeriodWithoutData_ShouldDeleteAdjustment()
    {
        var start = DateTimeOffset.Parse("2026-06-22T13:00:00+000"); // Monday
        var tuesday = start.AddDays(1);

        var @event = Event.Factory.MGWeekly(
            Period.New(start, start.AddHours(1)),
            TimeZoneInfo.Utc.Id,
            EventData.Test(),
            [DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday]);

        await Calendar.AddAsync(@event);
        await DbContext.SaveChangesAsync();

        var occurrenceId = new OccurrenceId(@event.Id, tuesday).ToString();

        await Calendar.MoveAsync(
            occurrenceId,
            Period.New(tuesday.AddHours(2), tuesday.AddHours(3)));
        await DbContext.SaveChangesAsync();

        var occurrenceAdjustmentBeforeMoveBack =
            await Calendar.ByIdAsync<OccurrenceAdjustment<EventData>>(occurrenceId);
        occurrenceAdjustmentBeforeMoveBack.Should().NotBeNull();
        occurrenceAdjustmentBeforeMoveBack!.MoveTo.Should().Be(Period.New(tuesday.AddHours(2), tuesday.AddHours(3)));
        occurrenceAdjustmentBeforeMoveBack.Data.Should().BeNull();

        await Calendar.MoveAsync(
            occurrenceId,
            Period.New(tuesday, tuesday.AddHours(1)));
        await DbContext.SaveChangesAsync();

        var occurrenceAdjustmentAfterMoveBack =
            await Calendar.ByIdAsync<OccurrenceAdjustment<EventData>>(occurrenceId);
        occurrenceAdjustmentAfterMoveBack.Should().BeNull();
    }

    [Test]
    public async Task WhenMoveOccurrenceAndGroupMoveInFutureExists_ShouldKeepFollowingOccurrences()
    {
        var timeZone = "America/New_York";
        var monday = DateTimeOffset.Parse("2026-06-22T08:00:00-04:00"); // Monday
        var wednesday = monday.AddDays(2);
        var thursday = monday.AddDays(3);
        var weekPeriod = Period.New(monday.StartOfDay(), monday.StartOfDay().AddDays(7));

        var @event = Event.Factory.MGWeekly(
            Period.New(monday, monday.AddHours(1)),
            timeZone,
            EventData.Test(),
            [
                DayOfWeek.Monday,
                DayOfWeek.Tuesday,
                DayOfWeek.Wednesday,
                DayOfWeek.Thursday,
                DayOfWeek.Friday,
                DayOfWeek.Saturday,
                DayOfWeek.Sunday
            ]);

        await Calendar.AddAsync(@event);
        await DbContext.SaveChangesAsync();

        var wednesdayOccurrenceId = new OccurrenceId(@event.Id, wednesday).ToString();

        await Calendar.MoveAsync(
            wednesdayOccurrenceId,
            Period.New(wednesday.AddHours(2), wednesday.AddHours(3)));
        await DbContext.SaveChangesAsync();

        var thursdayOccurrenceId = new OccurrenceId(@event.Id, thursday).ToString();

        await Calendar.UpdateOccurenceAsync(UpdateOccurrenceArgs<EventData>.NewMove(
            thursdayOccurrenceId,
            Period.New(thursday.AddHours(4), thursday.AddHours(5)),
            behavior: OccurenceUpdateBehavior.Group));
        await DbContext.SaveChangesAsync();

        await Calendar.UpdateOccurenceAsync(UpdateOccurrenceArgs<EventData>.NewMove(
            wednesdayOccurrenceId,
            Period.New(wednesday, wednesday.AddHours(1)),
            behavior: OccurenceUpdateBehavior.Group));
        await DbContext.SaveChangesAsync();

        var result = (await Calendar.OccurrencesAsync(weekPeriod))
            .OrderBy(x => x.Period.Start)
            .ToArray();

        result.Select(x => x.Period).Should().Equal(
            Period.New(monday, monday.AddHours(1)),
            Period.New(monday.AddDays(1), monday.AddDays(1).AddHours(1)),
            Period.New(wednesday, wednesday.AddHours(1)),
            Period.New(thursday, thursday.AddHours(1)),
            Period.New(monday.AddDays(4), monday.AddDays(4).AddHours(1)),
            Period.New(monday.AddDays(5), monday.AddDays(5).AddHours(1)),
            Period.New(monday.AddDays(6), monday.AddDays(6).AddHours(1)));
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
