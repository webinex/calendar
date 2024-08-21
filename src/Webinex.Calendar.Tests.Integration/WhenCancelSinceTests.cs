using FluentAssertions;
using Webinex.Calendar.Common;
using Webinex.Calendar.Events;
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
        var @event = RecurrentEvent<EventData>.NewWeekday(
            start: start,
            end: null,
            timeOfTheDayUtcMinutes: TimeSpan.FromHours(12).TotalMinutes.Round(),
            durationMinutes: TimeSpan.FromHours(1).TotalMinutes.Round(),
            weekdays: new[] { Weekday.Monday },
            timeZone: TimeZoneInfo.Utc.Id,
            new EventData("NAME"));

        await Calendar.Recurrent.AddAsync(@event);
        await DbContext.SaveChangesAsync();

        var eventsBefore = await Calendar.GetCalculatedAsync(searchPeriod.Start, searchPeriod.End);
        eventsBefore.Length.Should().Be(2);

        await Calendar.Recurrent.CancelAsync(@event.Id, start.AddDays(7));
        await DbContext.SaveChangesAsync();

        var eventsAfter = await Calendar.GetCalculatedAsync(searchPeriod.Start, searchPeriod.End);
        eventsAfter.Length.Should().Be(1);
        eventsAfter.Single().Start.Should().Be(start);
    }

    [SetUp]
    public new void SetUp()
    {
        CleanDatabase();
    }
}