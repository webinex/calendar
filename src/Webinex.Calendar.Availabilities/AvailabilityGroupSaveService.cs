using System.Diagnostics.CodeAnalysis;
using Webinex.Calendar.Common;
using Webinex.Calendar.Extensions;

namespace Webinex.Calendar.Availabilities;

internal class AvailabilityGroupSaveService<TData>
    where TData : class, IAvailabilityData
{
    private readonly IEventRepository<TData> _eventRepository;

    public AvailabilityGroupSaveService(IEventRepository<TData> eventRepository)
    {
        _eventRepository = eventRepository;
    }

    /// <summary>
    ///     Updates availability for a week and subsequent weeks.
    ///     When any event for this week or next exists it might be deleted
    /// </summary>
    /// <param name="args"><see cref="SaveAvailabilityArgs{TData}"/></param>
    public async Task SaveAsync(SaveAvailabilityArgs<TData> args)
    {
        var period = Period.New(args.StartOfWeek, args.StartOfWeek.AddDays(7)).InZone(args.TimeZone);
        var events = await _eventRepository.GetAvailabilityEventsAsync(args.TenantId, args.HostId);
        var operations = MapEventOperations(events, period).Concat(MapAddEventsOperations(args)).ToArray();
        await _eventRepository.PatchAsync(operations);
    }

    private IEnumerable<Operation> MapAddEventsOperations(SaveAvailabilityArgs<TData> args)
    {
        foreach (var item in args.Available)
        {
            var @event = Event.Factory.MGDaily(item.Period, args.TimeZone, item.Data, interval: 7);
            yield return Operation.Add(@event);
        }
    }

    private IEnumerable<Operation> MapEventOperations(
        IEnumerable<Event<TData>> events,
        Period<DateTimeOffset> period)
    {
        return events.SelectMany(x => MapEventOperations(x, period)).ToArray();
    }

    private IEnumerable<Operation> MapEventOperations(Event<TData> @event, Period<DateTimeOffset> period)
    {
        if (@event.Effective().End <= period.Start)
            return [];

        if (TryMapEventStartsInPastAndEndsOnThisWeekOrLater(@event, period, out var result1))
            return result1;

        if (TryMapEventStartsThisWeekOrInFuture(@event, period, out var result2))
            return result2;

        throw new InvalidOperationException(
            $"Unable to map operations for event {@event.Id} when save a week group for a period of {period}");
    }

    private bool TryMapEventStartsInPastAndEndsOnThisWeekOrLater(
        Event<TData> @event,
        Period<DateTimeOffset> period,
        [NotNullWhen(true)] out IEnumerable<Operation>? operations)
    {
        operations = null;
        var isEventStartsInPastAndEndsThisWeekOrLater =
            @event.Period.Start < period.Start &&
            (!@event.Effective().End.HasValue || @event.Effective().End!.Value > period.Start);

        if (!isEventStartsInPastAndEndsThisWeekOrLater)
            return false;

        @event.SetEndDate(period.End.ToDateOnly(@event.TimeZone).AddDays(-8)); // -8 because Until is inclusive
        operations = [Operation.Update(@event)];
        return true;
    }

    private bool TryMapEventStartsThisWeekOrInFuture(
        Event<TData> @event,
        Period<DateTimeOffset> period,
        [NotNullWhen(true)] out IEnumerable<Operation>? operations)
    {
        operations = null;
        var isEventStartsThisWeekOrInFuture = @event.Period.Start >= period.Start;

        if (!isEventStartsThisWeekOrInFuture)
            return false;

        operations = [Operation.Remove(@event)];
        return true;
    }
}