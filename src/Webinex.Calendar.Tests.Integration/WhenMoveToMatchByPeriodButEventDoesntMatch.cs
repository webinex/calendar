using FluentAssertions;
using Webinex.Calendar.Extensions;
using Webinex.Calendar.Tests.Integration.Setups;

namespace Webinex.Calendar.Tests.Integration;

[TestFixture]
public class WhenMoveToMatchByPeriodButEventDoesntMatch : IntegrationTestsBase
{
    private Event<EventData> _event = null!;
    private IReadOnlyCollection<Occurrence<EventData>> _occurrencesBefore = null!;
    private IReadOnlyCollection<Occurrence<EventData>> _occurrencesAfter = null!;

    [Test]
    public void OccurencesBefore_ShouldBeEmpty()
    {
        _occurrencesBefore.Count.Should().Be(0);
    }

    [Test]
    public void OccurencesAfter_ShouldIncludeMovedEvent()
    {
        _occurrencesAfter.Count.Should().Be(1);
        _occurrencesAfter.First().Period.Start.Should().Be(JAN1_2023_UTC.AddDays(2));
        _occurrencesAfter.First().Period.End.Should().Be(JAN1_2023_UTC.AddDays(2).AddHours(2));
    }
    
    [OneTimeSetUp]
    public async Task OneTimeSetUp()
    {
        CleanDatabase();

        _event = Event.Factory.MGDaily(
            Period.New(JAN1_2023_UTC, JAN1_2023_UTC.AddHours(1)),
            TimeZoneInfo.Utc.Id,
            EventData.Test(),
            interval: 3);

        await Calendar.AddAsync(_event);
        await DbContext.SaveChangesAsync();
        _occurrencesBefore = await Calendar.OccurrencesAsync(JAN1_2023_UTC.AddDays(2), JAN1_2023_UTC.AddDays(3));

        await Calendar.MoveAsync(
            new OccurrenceId(_event.Id, JAN1_2023_UTC).ToString(),
            Period.New(JAN1_2023_UTC.AddDays(2), JAN1_2023_UTC.AddDays(2).AddHours(2)));
        await DbContext.SaveChangesAsync();
        
        _occurrencesAfter = await Calendar.OccurrencesAsync(JAN1_2023_UTC.AddDays(2), JAN1_2023_UTC.AddDays(3));
    }
}