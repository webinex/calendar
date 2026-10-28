using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Webinex.Calendar.EntityFramework;
using Webinex.Calendar.Tests.Integration.Setups;

namespace Webinex.Calendar.Tests.Integration;

public class WhenMoveOneTimeEventTests : IntegrationTestsBase
{
    [TestCase(-7, false)]
    [TestCase(7, false)]
    [TestCase(-7, true)]
    [TestCase(7, true)]
    public async Task WhenMove_ShouldPersistAndBeVisibleInNewPeriod(int days, bool updateData)
    {
        var originalPeriod = Period.New(JAN1_2023_UTC, JAN1_2023_UTC.AddHours(1));
        var newPeriod = Period.New(originalPeriod.Start.AddDays(days), originalPeriod.End.AddDays(days));
        var @event = Event<EventData>.New(originalPeriod, TimeZoneInfo.Utc.Id, EventData.Test());
        var originalId = OccurrenceId.OneTime(@event.Id).ToString();

        await Calendar.AddAsync(@event);
        await DbContext.SaveChangesAsync();
        await Calendar.UpdateOccurenceAsync(new UpdateOccurrenceArgs<EventData>(
            originalId,
            period: new Optional<Period<DateTimeOffset>>(newPeriod),
            data: updateData ? new Optional<EventData?>(new EventData("UPDATED")) : null));
        await DbContext.SaveChangesAsync();

        using var scope = Services.CreateScope();
        var calendar = scope.ServiceProvider.GetRequiredService<ICalendar<EventData>>();
        var occurrence = await calendar.OccurrenceAsync(originalId);
        occurrence!.Period.Should().Be(newPeriod);
        occurrence.Id.Should().Be(OccurrenceId.OneTime(@event.Id).ToString());
        occurrence.Data.Name.Should().Be(updateData ? "UPDATED" : "NAME");
        var occurrences = await calendar.OccurrencesAsync(newPeriod.Start, newPeriod.End);
        occurrences.Should().ContainSingle().Which.Period.Should().Be(newPeriod);
        var originalOccurrences = await calendar.OccurrencesAsync(originalPeriod.Start, originalPeriod.End);
        originalOccurrences.Should().BeEmpty();

        var dbContext = scope.ServiceProvider.GetRequiredService<TestDbContext>();
        var row = await dbContext.Set<EventRow<EventData>>().SingleAsync(x => x.Id == @event.Id);
        row.Period.Should().Be(newPeriod);
        row.Effective.Should().Be(newPeriod);
    }

    [SetUp]
    public void SetUp()
    {
        CleanDatabase();
    }
}
