using FluentAssertions;
using Webinex.Calendar.Common;
using Webinex.Calendar.Events;
using Webinex.Calendar.Tests.Integration.Setups;

namespace Webinex.Calendar.Tests.Integration;

public class WhenGetAllDailyRepeatEventTests : IntegrationTestsBase
{
    [Test]
    public async Task WhenMatch_ShouldReturn()
    {
        var @event = RecurrentEvent<EventData>.NewDayOfMonth(
            Constants.J1_1990,
            null,
            timeOfTheDayUtcMinutes: 6 * 60,
            60,
            new DayOfMonth(25),
            TimeZoneInfo.Utc,
            new EventData("NAME"));

        await Calendar.Recurrent.AddAsync(@event);
        await DbContext.SaveChangesAsync();

        var from = JAN1_2023_UTC.AddDays(24);
        var to = from.AddHours(6).AddMinutes(1);

        var events = await Calendar.GetCalculatedAsync(from, to);
        events.Length.Should().Be(1);
    }

    [Test]
    public async Task WhenOneDayWithOffset_ShouldReturn()
    {
        var @event = RecurrentEvent<EventData>.NewDayOfMonth(
            Constants.J1_1990,
            null,
            timeOfTheDayUtcMinutes: 6 * 60,
            60,
            new DayOfMonth(25),
            TimeZoneInfo.Utc,
            new EventData("NAME"));

        await Calendar.Recurrent.AddAsync(@event);
        await DbContext.SaveChangesAsync();

        var events = await Calendar.GetCalculatedAsync(
            new DateTimeOffset(2022, 01, 25, 0, 0, 0, TimeSpan.FromHours(3)),
            new DateTimeOffset(2022, 01, 26, 0, 0, 0, TimeSpan.FromHours(3)));

        events.Length.Should().Be(1);
    }

    [SetUp]
    public new void SetUp()
    {
        CleanDatabase();
    }
}