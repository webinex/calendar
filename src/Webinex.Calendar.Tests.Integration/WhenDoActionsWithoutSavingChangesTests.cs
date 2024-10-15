using FluentAssertions;
using Webinex.Calendar.Common;
using Webinex.Calendar.Events;
using Webinex.Calendar.Tests.Integration.Common;
using Webinex.Calendar.Tests.Integration.Setups;

namespace Webinex.Calendar.Tests.Integration;

public class WhenDoActionsWithoutSavingChangesTests : IntegrationTestsBase
{
    [Test]
    public async Task WhenAddedRecurrentEvent_CalculationsShouldBeCorrect()
    {
        // Arrange
        var recurrentEvent = GetRecurrentEvent();

        // Act
        await Calendar.Recurrent.AddAsync(recurrentEvent);
        var items = await Calendar.GetCalculatedAsync(JAN1_2023_UTC, JAN1_2023_UTC.AddDays(14));

        // Assert
        items.Length.Should().Be(2);
    }

    [Test]
    public async Task WhenEventStateIsUpdated_CalculationsShouldBeCorrect()
    {
        // Arrange
        var recurrentEvent = GetRecurrentEvent();

        var updatedEventStart = JAN1_2023_UTC.WithTime("10:00".ToTimeOnly());
        var updatedEventNewData = new EventData("NEW NAME");

        // Act
        await Calendar.Recurrent.AddAsync(recurrentEvent);
        await Calendar.Recurrent.SaveDataAsync(recurrentEvent, updatedEventStart, updatedEventNewData);
        var items = await Calendar.GetCalculatedAsync(JAN1_2023_UTC, JAN1_2023_UTC.AddDays(14));

        // Assert
        items.Length.Should().Be(2);
        var changedEvent = items.FirstOrDefault(e => e.Start == updatedEventStart);

        changedEvent.Should().NotBeNull();
        changedEvent.Data.Name.Should().Be(updatedEventNewData.Name);

        items.Except(new[] { changedEvent }).Should()
            .AllSatisfy(e => e.Data.Name.Should().Be(recurrentEvent.Data.Name));
    }

    [Test]
    public async Task WhenEventStateIsSaved_DoMultipleUpdatesOfTheSameState_CalculationsShouldBeCorrect()
    {
        // Arrange
        var recurrentEvent = GetRecurrentEvent();
        var updatedEventStart = JAN1_2023_UTC.WithTime("10:00".ToTimeOnly());
        var updatedEventNewData1 = new EventData("NAME_1", new EventData.NestedValue("1"));
        var updatedEventNewData2 = new EventData("NAME_2", new EventData.NestedValue("2"));
        var updatedEventNewData3 = new EventData("NAME_3", new EventData.NestedValue("3"));

        // Act
        await Calendar.Recurrent.AddAsync(recurrentEvent);
        await Calendar.Recurrent.SaveDataAsync(recurrentEvent, updatedEventStart, updatedEventNewData1);
        await DbContext.SaveChangesAsync();
        DbContext.ChangeTracker.Clear();
        
        await Calendar.Recurrent.SaveDataAsync(recurrentEvent, updatedEventStart, updatedEventNewData2);
        await Calendar.Recurrent.SaveDataAsync(recurrentEvent, updatedEventStart, updatedEventNewData3);
        var items = await Calendar.GetCalculatedAsync(JAN1_2023_UTC, JAN1_2023_UTC.AddDays(14));

        // Assert
        items.Length.Should().Be(2);
        var changedEvent = items.FirstOrDefault(e => e.Start == updatedEventStart);

        changedEvent.Should().NotBeNull();
        changedEvent.Data.Name.Should().Be(updatedEventNewData3.Name);

        items.Except(new[] { changedEvent }).Should()
            .AllSatisfy(e => e.Data.Name.Should().Be(recurrentEvent.Data.Name));
    }
    
    [Test]
    public async Task WhenEventStateIsSaved_CancelEventState_CalculationsShouldBeCorrect()
    {
        // Arrange
        var recurrentEvent = GetRecurrentEvent();
        var updatedEventStart = JAN1_2023_UTC.WithTime("10:00".ToTimeOnly());
        var updatedEventNewData1 = new EventData("NAME_1", new EventData.NestedValue("1"));

        // Act
        await Calendar.Recurrent.AddAsync(recurrentEvent);
        await Calendar.Recurrent.SaveDataAsync(recurrentEvent, updatedEventStart, updatedEventNewData1);
        await Calendar.Recurrent.CancelAppearanceAsync(recurrentEvent, updatedEventStart);
        var items = await Calendar.GetCalculatedAsync(JAN1_2023_UTC, JAN1_2023_UTC.AddDays(14));

        // Assert
        items.Length.Should().Be(1);
        items.Should().AllSatisfy(e => e.Data.Name.Should().Be(recurrentEvent.Data.Name));
    }
    
    [Test]
    public async Task WhenEventStateIsSaved_DeleteEventState_CalculationsShouldBeCorrect()
    {
        // Arrange
        var recurrentEvent = GetRecurrentEvent();
        var updatedEventStart = JAN1_2023_UTC.WithTime("10:00".ToTimeOnly());
        var updatedEventNewData1 = new EventData("NAME_1", new EventData.NestedValue("1"));

        // Act
        await Calendar.Recurrent.AddAsync(recurrentEvent);
        await Calendar.Recurrent.SaveDataAsync(recurrentEvent, updatedEventStart, updatedEventNewData1);
        await Calendar.Recurrent.DeleteStateAsync(new RecurrentEventStateId(recurrentEvent.Id, updatedEventStart));
        var items = await Calendar.GetCalculatedAsync(JAN1_2023_UTC, JAN1_2023_UTC.AddDays(14));

        // Assert
        items.Length.Should().Be(2);
        items.Should().AllSatisfy(e => e.Data.Name.Should().Be(recurrentEvent.Data.Name));
    }

    private RecurrentEvent<EventData> GetRecurrentEvent() => RecurrentEvent<EventData>.NewWeekday(
        JAN1_2023_UTC,
        null,
        timeOfTheDayUtcMinutes: "10:00".ToTimeOnly().TotalMinutes(),
        durationMinutes: TimeSpan.FromHours(2).TotalMinutes.Round(),
        weekdays: new[] { Weekday.Sunday },
        timeZone: TimeZoneInfo.Utc.Id,
        new EventData("NAME_0", new EventData.NestedValue("0")));

    [SetUp]
    public new void SetUp() => CleanDatabase();
}