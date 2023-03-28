using FluentAssertions;
using Webinex.Calendar.Events;
using Webinex.Calendar.Tests.Integration.Common;
using Webinex.Calendar.Tests.Integration.Setups;

namespace Webinex.Calendar.Tests.Integration;

public class WhenCancelOneRecurrentEventTests : IntegrationTestsBase
{
    [Test]
    public async Task WhenCancel_ShouldExcept()
    {
        var @event = RecurrentEvent<EventData>.NewInterval(
            JAN1_2023_UTC,
            null,
            TimeSpan.FromHours(12).TotalMinutes.Round(),
            TimeSpan.FromHours(1).TotalMinutes.Round(),
            new EventData("NAME"));

        await Calendar.Recurrent.AddAsync(@event);
        await DbContext.SaveChangesAsync();

        var eventsBefore = await Calendar.GetCalculatedAsync(JAN1_2023_UTC, JAN1_2023_UTC.AddDays(1));
        eventsBefore.Length.Should().Be(2);
        
        await Calendar.Recurrent.CancelAppearanceAsync(@event, JAN1_2023_UTC.AddHours(12));
        await DbContext.SaveChangesAsync();

        var eventsAfter = await Calendar.GetCalculatedAsync(JAN1_2023_UTC, JAN1_2023_UTC.AddDays(1));
        eventsAfter.Length.Should().Be(1);
        eventsAfter.Single().Start.Should().Be(JAN1_2023_UTC);
    }

    [SetUp]
    public new void SetUp()
    {
        CleanDatabase();
    }
}