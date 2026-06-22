using Webinex.Asky;
using Webinex.Calendar.Common;
using Webinex.Calendar.Extensions;
using Webinex.Calendar.Services;

namespace Webinex.Calendar;

internal class Calendar<TData> : ICalendar<TData>
    where TData : class, ICloneable
{
    private readonly IEventRepository<TData> _eventRepository;
    private readonly IOccurrenceReadService<TData> _occurrenceReadService;
    private readonly IOccurrenceCancellationService<TData> _occurrenceCancellationService;
    private readonly IOccurrenceUpdateService<TData> _occurrenceUpdateService;

    public Calendar(
        IEventRepository<TData> eventRepository,
        IOccurrenceReadService<TData> occurrenceReadService,
        IOccurrenceCancellationService<TData> occurrenceCancellationService,
        IOccurrenceUpdateService<TData> occurrenceUpdateService)
    {
        _eventRepository = eventRepository;
        _occurrenceReadService = occurrenceReadService;
        _occurrenceCancellationService = occurrenceCancellationService;
        _occurrenceUpdateService = occurrenceUpdateService;
    }

    public async Task<T?> ByIdAsync<T>(string id) where T : IEventEntityBase
    {
        var result = await _eventRepository.ByIdAsync<T>([id]);
        return result.FirstOrDefault();
    }

    public async Task<IReadOnlyCollection<T>> ByIdAsync<T>(IEnumerable<string> ids) where T : IEventEntityBase
    {
        return await _eventRepository.ByIdAsync<T>(ids);
    }

    public async Task<IReadOnlyCollection<T>> AddRangeAsync<T>(IEnumerable<T> events)
        where T : IEvent<TData>
    {
        events = events.ToArray();
        foreach (var @event in events) @event.ValidateAtLeastOneOccurrenceOrThrow();
        var operations = events.Select(Operation.Add);
        var result = await _eventRepository.PatchAsync(operations);
        return result.Select(x => x.Value).Cast<T>().ToArray();
    }

    public async Task<IReadOnlyCollection<OccurrenceAdjustment<TData>>> AddRangeAsync(
        IEnumerable<OccurrenceAdjustment<TData>> states)
    {
        var result = await _eventRepository.PatchAsync(states.Select(Operation.Add));
        return result.Values.Select(x => (OccurrenceAdjustment<TData>)x).ToArray();
    }

    public async Task<IReadOnlyCollection<IEventEntityBase>> RemoveRangeAsync<T>(IEnumerable<T> events)
        where T : IEvent<TData>
    {
        events = events?.ToArray() ?? throw new ArgumentNullException(nameof(events));
        var recurrentEvents = events.OfType<Event<TData>>().ToArray();
        var states = await GetRelatedRecurrenceExceptionsAsync(recurrentEvents);
        var entities = ((IEnumerable<IEventEntityBase>)events).Concat(states).ToArray();
        return await _eventRepository.RemoveRangeAsync(entities);
    }

    private async Task<IReadOnlyCollection<OccurrenceAdjustment<TData>>> GetRelatedRecurrenceExceptionsAsync(
        IEnumerable<Event<TData>> events)
    {
        var ids = events.Select(x => x.Id).ToArray();
        var filterRule = FilterRule.In("recurrentEventId", ids);
        return await _eventRepository.GetAllAsync<OccurrenceAdjustment<TData>>(filterRule: filterRule);
    }

    public async Task<IReadOnlyCollection<OccurrenceAdjustment<TData>>> RemoveRangeAsync(
        IEnumerable<OccurrenceAdjustment<TData>> states)
    {
        return await _eventRepository.RemoveRangeAsync(states);
    }

    public async Task<IReadOnlyCollection<IEventEntityBase>> UpdateOccurrenceRangeAsync(
        IEnumerable<UpdateOccurrenceArgs<TData>> args)
    {
        return await _occurrenceUpdateService.UpdateOccurrenceRangeAsync(args);
    }

    public async Task CancelOccurrenceRangeAsync(IEnumerable<CancelOccurrenceArgs> args)
    {
        await _occurrenceCancellationService.CancelRangeAsync(args);
    }

    public async Task<IReadOnlyCollection<T>> GetAllAsync<T>(FilterRule? filterRule, IEnumerable<SortRule>? sortRules,
        PagingRule? pagingRule) where T : IEventEntityBase
    {
        return await _eventRepository.GetAllAsync<T>(filterRule, sortRules, pagingRule);
    }

    public async Task<IReadOnlyCollection<Occurrence<TData>>> OccurrencesAsync(
        Period<DateTimeOffset> period,
        FilterRule? dataFilterRule = null,
        bool tryCache = false)
    {
        return await _occurrenceReadService.OccurrencesAsync(period, dataFilterRule, tryCache);
    }

    public async Task<IReadOnlyCollection<Occurrence<TData>>> OccurrencesAsync(IEnumerable<string> ids, bool tryCache = false)
    {
        return await _occurrenceReadService.OccurrencesAsync(ids, tryCache);
    }

    public async Task<ILookup<string, Occurrence<TData>>> OccurrencesByEventAsync(OccurrencesByEventQueryArgs args)
    {
        return await _occurrenceReadService.OccurrencesByEventAsync(args);
    }

    public async Task<IReadOnlyCollection<EventGroup>> EventGroupAsync(IEnumerable<Guid> ids)
    {
        return await _eventRepository.EventGroupAsync(ids);
    }
}