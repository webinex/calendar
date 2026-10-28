using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.Extensions.DependencyInjection;
using Webinex.Calendar.Extensions;
using Webinex.Calendar.Tests.Integration.Setups;

namespace Webinex.Calendar.Tests.Integration;

public class WhenMoveDenverOccurrenceTests : IntegrationTestsBase
{
    [TestCase(27, 2, 0, false)]
    [TestCase(27, 1, 30, false)]
    [TestCase(28, 3, 0, false)]
    [TestCase(27, 2, 0, true)]
    [TestCase(27, 1, 30, true)]
    [TestCase(28, 3, 0, true)]
    public async Task WhenMoveBackAndRetry_ShouldPersistBothRequestedTimes(
        int originalDay, int originalHour, int originalMinute, bool existingAdjustment)
    {
        const string timeZone = "America/Denver";
        var zone = TimeZoneInfo.FindSystemTimeZoneById(timeZone);
        DateTimeOffset At(int day, int hour, int minute) => TimeZoneInfo.ConvertTime(
            new DateTimeOffset(2026, 10, day, hour, minute, 0, TimeSpan.FromHours(-6)), zone);

        var originalStart = At(originalDay, originalHour, originalMinute);
        var displayedStart = At(28, 3, 0);
        var firstTarget = At(27, 2, 0);
        var retryTarget = At(27, 1, 30);
        var @event = Event.Factory.MGDaily(
            Period.New(originalStart.AddDays(-7), originalStart.AddDays(-7).AddHours(1)),
            timeZone, EventData.Test(), interval: 7);
        var occurrenceId = OccurrenceId.Recurrent(@event.Id, originalStart).ToString();

        await Calendar.AddAsync(@event);
        await DbContext.SaveChangesAsync();
        if (existingAdjustment)
        {
            await Calendar.SaveDataAsync(occurrenceId, new EventData("UPDATED"));
            await DbContext.SaveChangesAsync();
        }
        if (originalStart != displayedStart)
        {
            await Calendar.MoveAsync(occurrenceId, Period.New(displayedStart, displayedStart.AddHours(1)));
            await DbContext.SaveChangesAsync();
        }

        var firstResult = await MoveAndReadAsync(firstTarget);
        var retryResult = await MoveAndReadAsync(retryTarget);

        TestContext.WriteLine($"Original={originalStart:O}; displayed={displayedStart:O}");
        TestContext.WriteLine($"First requested={firstTarget:O}; actual={firstResult.ById.Period.Start:O}");
        TestContext.WriteLine($"Retry requested={retryTarget:O}; actual={retryResult.ById.Period.Start:O}");
        TestContext.WriteLine($"Retry day={string.Join(",", retryResult.Day.Select(x => x.Period.Start.ToString("O")))}");
        TestContext.WriteLine($"Retry week={string.Join(",", retryResult.Week.Select(x => x.Period.Start.ToString("O")))}");
        using var assertions = new AssertionScope();
        firstResult.ById.Period.Start.Should().Be(firstTarget);
        retryResult.ById.Period.Start.Should().Be(retryTarget);
        firstResult.Day.Should().ContainSingle().Which.Period.Start.Should().Be(firstTarget);
        retryResult.Day.Should().ContainSingle().Which.Period.Start.Should().Be(retryTarget);
        firstResult.Week.Should().ContainSingle().Which.Period.Start.Should().Be(firstTarget);
        retryResult.Week.Should().ContainSingle().Which.Period.Start.Should().Be(retryTarget);

        async Task<MoveResult> MoveAndReadAsync(DateTimeOffset target)
        {
            using (var writeScope = Services.CreateScope())
            {
                var calendar = writeScope.ServiceProvider.GetRequiredService<ICalendar<EventData>>();
                await calendar.MoveAsync(occurrenceId, Period.New(target, target.AddHours(1)));
                await writeScope.ServiceProvider.GetRequiredService<TestDbContext>().SaveChangesAsync();
            }

            using var readScope = Services.CreateScope();
            var readCalendar = readScope.ServiceProvider.GetRequiredService<ICalendar<EventData>>();
            var byId = (await readCalendar.OccurrenceAsync(occurrenceId))!;
            var day = await readCalendar.OccurrencesAsync(At(27, 0, 0), At(28, 0, 0));
            var week = await readCalendar.OccurrencesAsync(At(25, 0, 0), At(31, 0, 0).AddDays(1));
            return new MoveResult(byId, day, week);
        }
    }

    private record MoveResult(
        Occurrence<EventData> ById,
        IReadOnlyCollection<Occurrence<EventData>> Day,
        IReadOnlyCollection<Occurrence<EventData>> Week);

    [SetUp]
    public void SetUp()
    {
        CleanDatabase();
    }
}
