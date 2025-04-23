using FluentAssertions;
using Webinex.Calendar.Tests.Integration.Setups;

namespace Webinex.Calendar.Tests.Integration;

public class WhenGetAllOneTimeEventTests : IntegrationTestsBase
{
    [Test]
    public async Task WhenMatch_ShouldReturn()
    {
        var @event = Event<EventData>.New(
            new Period<DateTimeOffset>(JAN1_2023_UTC.AddHours(5), JAN1_2023_UTC.AddHours(6)),
            TimeZoneInfo.Utc.Id,
            new EventData("NAME"));
        
        await Calendar.AddAsync(@event);
        await DbContext.SaveChangesAsync();

        var occurrences = await Calendar.OccurrencesAsync(JAN1_2023_UTC, JAN1_2023_UTC.AddDays(1));
        occurrences.Count.Should().Be(1);
    }

    [Test]
    public async Task WhenNotMatch_ShouldBeEmpty()
    {
        var @event = Event<EventData>.New(
            new Period<DateTimeOffset>(JAN1_2023_UTC.AddHours(5), JAN1_2023_UTC.AddHours(6)),
            TimeZoneInfo.Utc.Id,
            new EventData("NAME"));

        await Calendar.AddAsync(@event);
        await DbContext.SaveChangesAsync();

        var occurrences = await Calendar.OccurrencesAsync(JAN1_2023_UTC.AddHours(6), JAN1_2023_UTC.AddDays(1));
        occurrences.Should().BeEmpty();
    }

    [SetUp]
    public void SetUp()
    {
        CleanDatabase();
    }
}