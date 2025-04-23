using FluentAssertions;
using Webinex.Asky;
using Webinex.Calendar.Extensions;
using Webinex.Calendar.MicrosoftGraph;
using Webinex.Calendar.Tests.Integration.Common;
using Webinex.Calendar.Tests.Integration.Setups;

namespace Webinex.Calendar.Tests.Integration;

public class WhenGetAllRecurrentEventsWithDataFilterTests : IntegrationTestsBase
{
    [Test]
    public async Task WhenRangeMatchEventAndNoState_EventPredicateMatch_ShouldReturn()
    {
        var @event = Event.Factory.MGDaily(
            new Period<DateTimeOffset>(JAN1_2023_UTC, JAN1_2023_UTC.AddMinutes(30)),
            "UTC",
            EventData.Test());

        await Calendar.AddAsync(@event);
        await DbContext.SaveChangesAsync();

        var occurrences = await Calendar.OccurrencesAsync(
            JAN1_2023_UTC,
            JAN1_2023_UTC.AddHours(12),
            FilterRule.Eq("name", "NAME"));

        occurrences.Count.Should().Be(1);
    }

    [Test]
    public async Task WhenRangeMatchEventAndState_StateMatchPredicateButEventDontMatchPredicate_ShouldReturn()
    {
        var @event = Event.Factory.MGDaily(
            new Period<DateTimeOffset>(JAN1_2023_UTC, JAN1_2023_UTC.AddMinutes(30)),
            "UTC",
            EventData.Test());

        await Calendar.AddAsync(@event);
        await DbContext.SaveChangesAsync();

        var occurrenceId = new OccurrenceId(@event.Id, JAN1_2023_UTC);
        await Calendar.SaveDataAsync(occurrenceId.ToString(), new EventData("NEW_NAME"));
        await DbContext.SaveChangesAsync();

        var events = await Calendar.OccurrencesAsync(
            JAN1_2023_UTC,
            JAN1_2023_UTC.AddHours(12),
            FilterRule.Eq("name", "NEW_NAME"));

        events.Count.Should().Be(1);
    }

    [Test]
    public async Task WhenRangeMatchEventAndState_StateDoesntMatchPredicateButEventMatchPredicate_ShouldReturn()
    {
        
        var @event = Event.Factory.MGDaily(
            new Period<DateTimeOffset>(JAN1_2023_UTC, JAN1_2023_UTC.AddMinutes(30)),
            "UTC",
            EventData.Test());

        await Calendar.AddAsync(@event);
        await DbContext.SaveChangesAsync();

        await Calendar.SaveDataAsync(new OccurrenceId(@event.Id, JAN1_2023_UTC).ToString(), new EventData("NEW_NAME"));
        await DbContext.SaveChangesAsync();

        var events = await Calendar.OccurrencesAsync(
            JAN1_2023_UTC,
            JAN1_2023_UTC.AddHours(12),
            FilterRule.Eq("name", "NAME"));

        events.Count.Should().Be(0);
    }

    [SetUp]
    public new void SetUp()
    {
        CleanDatabase();
    }
}