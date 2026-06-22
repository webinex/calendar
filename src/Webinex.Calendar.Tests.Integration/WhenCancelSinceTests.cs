using FluentAssertions;
using Webinex.Calendar.Extensions;
using Webinex.Calendar.Tests.Integration.Common;
using Webinex.Calendar.Tests.Integration.Setups;

namespace Webinex.Calendar.Tests.Integration;

public class WhenCancelSinceTests : IntegrationTestsBase
{
    [Test]
    public async Task WhenCancel_ShouldExcept()
    {
        var start = DateTimeOffset.Parse("2024-08-19T12:00:00+000"); // Monday
        var searchPeriod = (Start: start.AddDays(-7), End: start.AddDays(14));

        var @event = Event.Factory.MGWeekly(
            new Period<DateTimeOffset>(start, start.AddHours(1)),
            TimeZoneInfo.Utc.Id,
            EventData.Test(),
            [DayOfWeek.Monday]);

        await Calendar.AddAsync(@event);
        await DbContext.SaveChangesAsync();

        var eventsBefore = await Calendar.OccurrencesAsync(searchPeriod.Start, searchPeriod.End);
        eventsBefore.Count.Should().Be(2);

        await Calendar.CancelOccurrenceAsync(eventsBefore.First(x => x.Period.Start == start.AddDays(7)).Id);
        await DbContext.SaveChangesAsync();

        var eventsAfter = await Calendar.OccurrencesAsync(searchPeriod.Start, searchPeriod.End);
        eventsAfter.Count.Should().Be(1);
        eventsAfter.Single().Period.Start.Should().Be(start);
    }

    [Test]
    public async Task WhenCancelGroupOnFirstActualOccurrence_ShouldDelete()
    {
        var start = DateTimeOffset.Parse("2026-06-17T11:00:00+000"); // Wed

        var @event = Event.Factory.MGWeekly(
            Period.New(start, start.AddHours(1)),
            TimeZoneInfo.Utc.Id,
            EventData.Test(),
            [DayOfWeek.Monday, DayOfWeek.Tuesday]);

        await Calendar.AddAsync(@event);
        await DbContext.SaveChangesAsync();

        var eventBefore = await Calendar.ByIdAsync<IEventEntityBase>(@event.Id);
        eventBefore.Should().NotBeNull();
        var occurrencesBefore = await Calendar.OccurrencesAsync(start.StartOfThisOrNext(DayOfWeek.Monday),
            start.StartOfThisOrNext(DayOfWeek.Monday).AddDays(1));

        var firstOccurrence = occurrencesBefore.OrderBy(x => x.Period.Start).First();

        await Calendar.CancelOccurrenceAsync(
            firstOccurrence.Id,
            OccurenceUpdateBehavior.Group);

        await DbContext.SaveChangesAsync();

        var eventAfter = await Calendar.ByIdAsync<IEventEntityBase>(@event.Id);
        eventAfter.Should().BeNull();
    }

    [SetUp]
    public void SetUp()
    {
        CleanDatabase();
    }
}