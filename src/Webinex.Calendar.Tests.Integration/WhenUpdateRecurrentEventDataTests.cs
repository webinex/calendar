using FluentAssertions;
using Webinex.Calendar.Common;
using Webinex.Calendar.Events;
using Webinex.Calendar.Tests.Integration.Setups;

namespace Webinex.Calendar.Tests.Integration;

public class WhenUpdateRecurrentEventDataTests : IntegrationTestsBase
{
    [Test]
    public async Task WhenMatch_ShouldReturn()
    {
        var @event = RecurrentEvent<EventData>.NewWeekday(
            JAN1_2023_UTC,
            null,
            (int)TimeSpan.FromHours(6).TotalMinutes,
            (int)TimeSpan.FromHours(1).TotalMinutes,
            new[] { Weekday.Sunday, Weekday.Tuesday },
            TimeZoneInfo.Utc,
            new EventData("NAME"));

        await Calendar.Recurrent.AddAsync(@event);
        await DbContext.SaveChangesAsync();

        await Calendar.Recurrent.AddDataAsync(@event, JAN1_2023_UTC.AddHours(6), new EventData("NEW_NAME"));
        await DbContext.SaveChangesAsync();

        var events = await Calendar.GetCalculatedAsync(JAN1_2023_UTC, JAN1_2023_UTC.AddDays(2).AddHours(6).AddMinutes(1));
        events.Length.Should().Be(2);

        events.OrderBy(x => x.Start).ElementAt(0).Data.Name.Should().Be("NEW_NAME");
        events.OrderBy(x => x.Start).ElementAt(1).Data.Name.Should().Be("NAME");
    }

    [SetUp]
    public new void SetUp()
    {
        CleanDatabase();
    }
}