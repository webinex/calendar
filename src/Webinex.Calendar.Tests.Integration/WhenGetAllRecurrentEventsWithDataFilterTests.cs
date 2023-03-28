using FluentAssertions;
using Webinex.Asky;
using Webinex.Calendar.Events;
using Webinex.Calendar.Tests.Integration.Setups;

namespace Webinex.Calendar.Tests.Integration;

public class WhenGetAllRecurrentEventsWithDataFilterTests : IntegrationTestsBase
{
    [Test]
    public async Task WhenRangeMatchEventAndNoState_EventPredicateMatch_ShouldReturn()
    {
        var @event = RecurrentEvent<EventData>.NewInterval(
            JAN1_2023_UTC,
            null,
            intervalMinutes: 24 * 60,
            durationMinutes: 30,
            new EventData("NAME"));

        await Calendar.Recurrent.AddAsync(@event);
        await DbContext.SaveChangesAsync();

        var events = await Calendar.GetCalculatedAsync(
            JAN1_2023_UTC,
            JAN1_2023_UTC.AddHours(12),
            FilterRule.Eq("name", "NAME"));

        events.Length.Should().Be(1);
    }

    [Test]
    public async Task WhenRangeMatchEventAndState_StateMatchPredicateButEventDontMatchPredicate_ShouldReturn()
    {
        var @event = RecurrentEvent<EventData>.NewInterval(
            JAN1_2023_UTC,
            null,
            intervalMinutes: 24 * 60,
            durationMinutes: 30,
            new EventData("NAME"));

        await Calendar.Recurrent.AddAsync(@event);
        await DbContext.SaveChangesAsync();

        await Calendar.Recurrent.AddDataAsync(@event, JAN1_2023_UTC, new EventData("NEW_NAME"));
        await DbContext.SaveChangesAsync();

        var events = await Calendar.GetCalculatedAsync(
            JAN1_2023_UTC,
            JAN1_2023_UTC.AddHours(12),
            FilterRule.Eq("name", "NEW_NAME"));

        events.Length.Should().Be(1);
    }

    [Test]
    public async Task WhenRangeMatchEventAndState_StateDoesntMatchPredicateButEventMatchPredicate_ShouldReturn()
    {
        var @event = RecurrentEvent<EventData>.NewInterval(
            JAN1_2023_UTC,
            null,
            intervalMinutes: 24 * 60,
            durationMinutes: 30,
            new EventData("NAME"));

        await Calendar.Recurrent.AddAsync(@event);
        await DbContext.SaveChangesAsync();

        await Calendar.Recurrent.AddDataAsync(@event, JAN1_2023_UTC, new EventData("NEW_NAME"));
        await DbContext.SaveChangesAsync();

        var events = await Calendar.GetCalculatedAsync(
            JAN1_2023_UTC,
            JAN1_2023_UTC.AddHours(12),
            FilterRule.Eq("name", "NAME"));

        events.Length.Should().Be(0);
    }

    [SetUp]
    public new void SetUp()
    {
        CleanDatabase();
    }
}