using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Webinex.Asky;
using Webinex.Calendar.Extensions;
using Webinex.Calendar.Tests.Integration.Setups;

namespace Webinex.Calendar.Tests.Integration;

public class WhenMoveOneTimeEventIdTests : IntegrationTestsBase
{
    private static readonly Period<DateTimeOffset> OriginalPeriod = Period.New(JAN1_2023_UTC, JAN1_2023_UTC.AddHours(1));

    [Test]
    public async Task WhenMovedTwice_ShouldKeepIdInAllQueries()
    {
        var @event = await AddEventAsync();
        var id = OccurrenceId.OneTime(@event.Id).ToString();
        var secondPeriod = OriginalPeriod.Move(TimeSpan.FromDays(3));

        await Calendar.MoveAsync(id, OriginalPeriod.Move(TimeSpan.FromDays(1)));
        await DbContext.SaveChangesAsync();
        await Calendar.MoveAsync(id, secondPeriod);
        await DbContext.SaveChangesAsync();

        using var scope = Services.CreateScope();
        var calendar = scope.ServiceProvider.GetRequiredService<ICalendar<EventData>>();

        var byId = await calendar.OccurrenceAsync(id);
        byId!.Id.Should().Be(id);
        byId.Period.Should().Be(secondPeriod);

        var byPeriod = await calendar.OccurrencesAsync(secondPeriod.Start, secondPeriod.End);
        byPeriod.Should().ContainSingle().Which.Id.Should().Be(id);

        var materialized = await calendar.MaterializedOccurrencesAsync(FilterRule.Eq("id", @event.Id));
        materialized.Should().ContainSingle().Which.Id.Should().Be(id);
    }

    [TestCase(0)]
    [TestCase(1)]
    public async Task WhenRequestedByLegacyIdWithStart_ShouldResolveEvent(int legacyStartShiftDays)
    {
        var @event = await AddEventAsync();
        var movedPeriod = OriginalPeriod.Move(TimeSpan.FromDays(1));
        await Calendar.MoveAsync(OccurrenceId.OneTime(@event.Id).ToString(), movedPeriod);
        await DbContext.SaveChangesAsync();
        // Ids issued before contain start of the event (original or after move)
        var legacyId = OccurrenceId.UnsafeLegacyOneTimeShouldNotBeUsed(@event.Id, OriginalPeriod.Start.AddDays(legacyStartShiftDays)).ToString();

        using var scope = Services.CreateScope();
        var calendar = scope.ServiceProvider.GetRequiredService<ICalendar<EventData>>();
        var occurrence = await calendar.OccurrenceAsync(legacyId);

        occurrence!.Id.Should().Be(OccurrenceId.OneTime(@event.Id).ToString());
        occurrence.Period.Should().Be(movedPeriod);
    }

    [Test]
    public async Task WhenMovedByLegacyId_ShouldMove()
    {
        var @event = await AddEventAsync();
        var legacyId = OccurrenceId.UnsafeLegacyOneTimeShouldNotBeUsed(@event.Id, OriginalPeriod.Start).ToString();
        var movedPeriod = OriginalPeriod.Move(TimeSpan.FromDays(1));

        await Calendar.MoveAsync(legacyId, movedPeriod);
        await DbContext.SaveChangesAsync();

        using var scope = Services.CreateScope();
        var calendar = scope.ServiceProvider.GetRequiredService<ICalendar<EventData>>();
        var occurrence = await calendar.OccurrenceAsync(OccurrenceId.OneTime(@event.Id).ToString());
        occurrence!.Period.Should().Be(movedPeriod);
    }

    private async Task<Event<EventData>> AddEventAsync()
    {
        var @event = Event<EventData>.New(OriginalPeriod, TimeZoneInfo.Utc.Id, EventData.Test());
        await Calendar.AddAsync(@event);
        await DbContext.SaveChangesAsync();
        return @event;
    }

    [SetUp]
    public void SetUp()
    {
        CleanDatabase();
    }
}
