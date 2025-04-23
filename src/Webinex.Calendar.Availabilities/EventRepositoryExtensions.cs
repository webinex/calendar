using Webinex.Asky;

namespace Webinex.Calendar.Availabilities;

internal static class EventRepositoryExtensions
{
    public static async Task<IReadOnlyCollection<Event<TData>>> GetAvailabilityEventsAsync<TData>(
        this IEventRepository<TData> eventRepository,
        string tenantId,
        string hostId)
        where TData : class, IAvailabilityData
    {
        var events = await eventRepository.GetAllAsync<Event<TData>>(
            FilterRule.And(FilterRule.Eq("data.tenantId", tenantId), FilterRule.Eq("data.hostId", hostId)));

        AssertRecurrentEvents(events);
        return events;
    }

    private static void AssertRecurrentEvents<TData>(IEnumerable<Event<TData>> events)
        where TData : class, IAvailabilityData
    {
        if (events.Any(x => x.Recurrence == null))
            throw new InvalidOperationException("Week occurrence save expects only recurrent events");
    }
}