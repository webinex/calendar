using FluentAssertions;
using Webinex.Calendar.Extensions;
using Webinex.Calendar.Tests.Integration.Setups;

namespace Webinex.Calendar.Tests.Integration;

public class WhenGetAllDailyRepeatEventTests : IntegrationTestsBase
{
    [Test]
    public async Task WhenMatch_ShouldReturn()
    {
        var period = new Period<DateTimeOffset>(
            JAN1_2023_UTC.AddHours(6),
            JAN1_2023_UTC.AddHours(6).AddHours(1));

        var @event = Event.Factory.MGAbsoluteMonthly(
            period,
            TimeZoneInfo.Utc.Id,
            EventData.Test(),
            dayOfMonth: 25);

        await Calendar.AddAsync(@event);
        await DbContext.SaveChangesAsync();

        var from = JAN1_2023_UTC.AddDays(24);
        var to = from.AddHours(6).AddMinutes(1);

        var events = await Calendar.OccurrencesAsync(from, to);
        events.Count.Should().Be(1);
    }

    [Test]
    public async Task WhenOneDayWithOffset_ShouldReturn()
    {
        var period = new Period<DateTimeOffset>(JAN1_2023_UTC.AddYears(-10).AddHours(6), JAN1_2023_UTC.AddYears(-10).AddHours(6).AddHours(1));
        
        var @event = Event.Factory.MGAbsoluteMonthly(
            period,
            TimeZoneInfo.Utc.Id,
            EventData.Test(),
            dayOfMonth: 25);

        await Calendar.AddAsync(@event);
        await DbContext.SaveChangesAsync();

        var events = await Calendar.OccurrencesAsync(
            new DateTimeOffset(2022, 01, 25, 0, 0, 0, TimeSpan.FromHours(3)),
            new DateTimeOffset(2022, 01, 26, 0, 0, 0, TimeSpan.FromHours(3)));

        events.Count.Should().Be(1);
    }

    [SetUp]
    public new void SetUp()
    {
        CleanDatabase();
    }
}