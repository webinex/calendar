using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Webinex.Calendar.Availabilities;
using Webinex.Calendar.EntityFramework;
using Webinex.Calendar.Extensions;
using Webinex.Calendar.MicrosoftGraph;
using Webinex.Calendar.Tests.Integration.Setups;

namespace Webinex.Calendar.Tests.Integration;

public class WhenSaveAvailabilityTests : IntegrationTestsBase
{
    [Test]
    public async Task WhenSavingOneDayAvailabilityAsOccurrence_ShouldPersistAndReturnOnlyThatDay()
    {
        const string tenantId = "e537139c-ee11-4eaf-b8c9-e79998666c60";
        const string hostId = "d4f51b02-2b04-4c10-888a-d67bbc94b40a";
        const string timeZone = "America/Indiana/Indianapolis";

        var saveArgs = new SaveAvailabilityArgs<AvailabilityData>(
            tenantId,
            hostId,
            DateOnly.Parse("2026-09-06"),
            timeZone,
            [new SaveAvailabilityArgs<AvailabilityData>.Item(
                Period.New(
                    DateTimeOffset.Parse("2026-09-08T04:00:00+00:00"),
                    DateTimeOffset.Parse("2026-09-08T06:00:00+00:00")),
                new AvailabilityData(tenantId, hostId))],
            OccurenceUpdateBehavior.Occurrence);

        var availability = Services.GetRequiredService<IAvailability<AvailabilityData>>();
        await availability.SaveAsync(saveArgs);
        await DbContext.SaveChangesAsync();

        var recurrentEvent = await DbContext.Set<RecurrentEventRow<AvailabilityData>>().AsNoTracking().SingleAsync();
        var availabilityCalendar = Services.GetRequiredService<ICalendar<AvailabilityData>>();
        var occurrences = await availabilityCalendar.OccurrencesAsync(
            DateTimeOffset.Parse("2026-09-06T04:00:00+00:00"),
            DateTimeOffset.Parse("2026-09-13T04:00:00+00:00"));

        recurrentEvent.TimeZone.Should().Be(timeZone);
        recurrentEvent.Data.HostId.Should().Be(hostId);
        recurrentEvent.Data.TenantId.Should().Be(tenantId);
        recurrentEvent.Period.Should().Be(Period.New(
            DateTimeOffset.Parse("2026-09-08T04:00:00+00:00"),
            DateTimeOffset.Parse("2026-09-08T06:00:00+00:00")));
        recurrentEvent.Effective.Start.Should().Be(DateTimeOffset.Parse("2026-09-08T04:00:00+00:00"));
        recurrentEvent.Effective.End.Should().Be(DateTimeOffset.Parse("2026-09-08T06:00:00+00:00"));
        recurrentEvent.Recurrence.MGRecurrence!.Period.Start.Should().Be(DateOnly.Parse("2026-09-08"));
        recurrentEvent.Recurrence.MGRecurrence.Period.End.Should().Be(DateOnly.Parse("2026-09-08"));
        recurrentEvent.Recurrence.MGRecurrence.Pattern.Type.Should().Be(RecurrenceType.Daily);
        recurrentEvent.Recurrence.MGRecurrence.Pattern.Interval.Should().Be(1);
        occurrences.Should().ContainSingle()
            .Which.Period.Should().Be(Period.New(
                DateTimeOffset.Parse("2026-09-08T04:00:00+00:00"),
                DateTimeOffset.Parse("2026-09-08T06:00:00+00:00")));
    }

    [SetUp]
    public void SetUp()
    {
        DbContext.Set<EventRow<AvailabilityData>>().ExecuteDelete();
        DbContext.Set<RecurrentEventRow<AvailabilityData>>().ExecuteDelete();
    }
}
