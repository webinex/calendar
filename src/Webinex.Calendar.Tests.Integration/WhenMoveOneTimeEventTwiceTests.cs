using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Webinex.Calendar.Tests.Integration.Setups;

namespace Webinex.Calendar.Tests.Integration;

public class WhenMoveOneTimeEventTwiceTests : IntegrationTestsBase
{
    [TestCase(false)]
    [TestCase(true)]
    public async Task WhenMoveTwice_ShouldPersistBothMovesWithOriginalOrUpdatedId(bool useUpdatedId)
    {
        var offset = TimeSpan.FromHours(-6);
        var originalStart = new DateTimeOffset(2026, 10, 28, 3, 0, 0, offset);
        var firstTarget = new DateTimeOffset(2026, 10, 27, 2, 0, 0, offset);
        var retryTarget = new DateTimeOffset(2026, 10, 27, 1, 30, 0, offset);
        var @event = Event<EventData>.New(
            Period.New(originalStart, originalStart.AddHours(1)), "America/Denver", EventData.Test());
        var originalId = OccurrenceId.OneTime(@event.Id).ToString();
        var requestId = originalId;

        await Calendar.AddAsync(@event);
        await DbContext.SaveChangesAsync();

        foreach (var target in new[] { firstTarget, retryTarget })
        {
            using (var writeScope = Services.CreateScope())
            {
                var calendar = writeScope.ServiceProvider.GetRequiredService<ICalendar<EventData>>();
                await calendar.MoveAsync(requestId, Period.New(target, target.AddHours(1)));
                await writeScope.ServiceProvider.GetRequiredService<TestDbContext>().SaveChangesAsync();
            }

            using var readScope = Services.CreateScope();
            var readCalendar = readScope.ServiceProvider.GetRequiredService<ICalendar<EventData>>();
            var occurrence = await readCalendar.OccurrenceAsync(originalId);
            occurrence!.Period.Start.Should().Be(target);
            var dayStart = new DateTimeOffset(2026, 10, 27, 0, 0, 0, offset);
            var day = await readCalendar.OccurrencesAsync(dayStart, dayStart.AddDays(1));
            day.Should().ContainSingle().Which.Period.Start.Should().Be(target);
            var oldDay = await readCalendar.OccurrencesAsync(dayStart.AddDays(1), dayStart.AddDays(2));
            oldDay.Should().BeEmpty();
            TestContext.WriteLine($"Requested={target:O}; by ID and calendar={occurrence.Period.Start:O}");
            if (useUpdatedId)
                requestId = occurrence.Id;
        }
    }

    [SetUp]
    public void SetUp()
    {
        CleanDatabase();
    }
}
