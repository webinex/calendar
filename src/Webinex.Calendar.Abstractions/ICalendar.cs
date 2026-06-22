using Webinex.Asky;

namespace Webinex.Calendar;

public interface ICalendar<TData>
    where TData : class, ICloneable
{
    Task<T?> ByIdAsync<T>(string id) where T : IEventEntityBase;

    Task<IReadOnlyCollection<T>> ByIdAsync<T>(IEnumerable<string> ids)
        where T : IEventEntityBase;

    Task<IReadOnlyCollection<T>> AddRangeAsync<T>(IEnumerable<T> events)
        where T : IEvent<TData>;

    Task<IReadOnlyCollection<OccurrenceAdjustment<TData>>> AddRangeAsync(
        IEnumerable<OccurrenceAdjustment<TData>> states);

    Task<IReadOnlyCollection<IEventEntityBase>> RemoveRangeAsync<T>(IEnumerable<T> events)
        where T : IEvent<TData>;

    Task<IReadOnlyCollection<OccurrenceAdjustment<TData>>> RemoveRangeAsync(
        IEnumerable<OccurrenceAdjustment<TData>> states);

    Task<IReadOnlyCollection<IEventEntityBase>> UpdateOccurrenceRangeAsync(
        IEnumerable<UpdateOccurrenceArgs<TData>> args);

    Task CancelOccurrenceRangeAsync(IEnumerable<CancelOccurrenceArgs> args);

    Task<IReadOnlyCollection<T>> GetAllAsync<T>(
        FilterRule? filterRule = null,
        IEnumerable<SortRule>? sortRules = null,
        PagingRule? pagingRule = null)
        where T : IEventEntityBase;

    Task<IReadOnlyCollection<Occurrence<TData>>> OccurrencesAsync(
        Period<DateTimeOffset> period,
        FilterRule? dataFilterRule = null,
        bool tryCache = false);

    /// <summary>
    ///     Returns only materialized occurrences: one-time events and occurrences with modified state.
    ///     Does not include generated occurrences for recurrent events.
    /// </summary>
    /// <param name="filterRule">Filtering criteria</param>
    /// <param name="sortRules">Sorting criteria</param>
    /// <param name="pagingRule">Paging criteria</param>
    /// <returns>Collection of materialized occurrences.</returns>
    Task<IReadOnlyCollection<Occurrence<TData>>> MaterializedOccurrencesAsync(
        FilterRule? filterRule = null,
        IEnumerable<SortRule>? sortRules = null,
        PagingRule? pagingRule = null);

    Task<IReadOnlyCollection<Occurrence<TData>>> OccurrencesAsync(IEnumerable<string> ids, bool tryCache = false);

    Task<ILookup<string, Occurrence<TData>>> OccurrencesByEventAsync(OccurrencesByEventQueryArgs args);

    Task<IReadOnlyCollection<EventGroup>> EventGroupAsync(IEnumerable<Guid> ids);
}

public static class CalendarExtensions
{
    public static async Task CancelOccurrenceAsync<TData>(
        this ICalendar<TData> calendar,
        string id,
        OccurenceUpdateBehavior behavior = OccurenceUpdateBehavior.Occurrence)
        where TData : class, ICloneable
    {
        await calendar.CancelOccurrenceRangeAsync([new CancelOccurrenceArgs(id, behavior)]);
    }

    public static async Task<Event<TData>> AddAsync<TData>(this ICalendar<TData> calendar, Event<TData> value)
        where TData : class, ICloneable
    {
        var result = await calendar.AddRangeAsync([value]);
        return result.Single();
    }

    public static async Task<IReadOnlyCollection<Occurrence<TData>>> OccurrencesAsync<TData>(
        this ICalendar<TData> calendar,
        DateTimeOffset start,
        DateTimeOffset end,
        FilterRule? dataFilterRule = null,
        bool tryCache = false)
        where TData : class, ICloneable
    {
        return await calendar.OccurrencesAsync(new Period<DateTimeOffset>(start, end), dataFilterRule, tryCache);
    }

    public static async Task<IEventEntityBase> SaveDataAsync<TData>(
        this ICalendar<TData> calendar,
        string occurrenceId,
        TData? data)
        where TData : class, ICloneable
    {
        var args = UpdateOccurrenceArgs<TData>.NewData(occurrenceId, data);
        var result = await calendar.UpdateOccurrenceRangeAsync([args]);
        return result.Single();
    }

    public static async Task<IEventEntityBase> PatchDataAsync<TData>(
        this ICalendar<TData> calendar,
        string occurrenceId,
        Func<TData, TData> patch)
        where TData : class, ICloneable
    {
        var occurrenceIdInstance = OccurrenceId.Parse(occurrenceId);

        switch (EventId.TypeOf(occurrenceIdInstance.EventId))
        {
            case EventType.OneTime:
            {
                var @event = await calendar.ByIdAsync<Event<TData>>(occurrenceIdInstance.EventId)
                             ?? throw new InvalidOperationException($"Event {occurrenceIdInstance.EventId} not found");
                var newData = patch(@event.Data);
                return await calendar.SaveDataAsync(occurrenceId, newData);
            }

            case EventType.Recurrent:
            {
                var occurrenceAdjustment = await calendar.ByIdAsync<OccurrenceAdjustment<TData>>(occurrenceId);
                if (occurrenceAdjustment?.Data == null)
                {
                    var @event = await calendar.ByIdAsync<Event<TData>>(occurrenceIdInstance.EventId)
                                 ?? throw new InvalidOperationException(
                                     $"Event {occurrenceIdInstance.EventId} not found");
                    var newData = patch(@event.Data);
                    return await calendar.SaveDataAsync(occurrenceId, newData);
                }
                else
                {
                    var newData = patch(occurrenceAdjustment.Data);
                    return await calendar.SaveDataAsync(occurrenceId, newData);
                }
            }

            default:
                throw new ArgumentException($"Occurrence id {occurrenceId} format incorrect", nameof(occurrenceId));
        }
    }

    public static async Task<IEventEntityBase> MoveAsync<TData>(
        this ICalendar<TData> calendar,
        string id,
        Period<DateTimeOffset> newPeriod)
        where TData : class, ICloneable
    {
        var args = UpdateOccurrenceArgs<TData>.NewMove(id, newPeriod);
        var result = await calendar.UpdateOccurrenceRangeAsync([args]);
        return result.Single();
    }

    public static async Task<Occurrence<TData>?> OccurrenceAsync<TData>(
        this ICalendar<TData> calendar,
        string id,
        bool tryCache = false)
        where TData : class, ICloneable
    {
        var result = await calendar.OccurrencesAsync([id], tryCache: tryCache);
        return result.FirstOrDefault();
    }

    public static async Task<IReadOnlyCollection<IEventEntityBase>> UpdateOccurenceAsync<TData>(
        this ICalendar<TData> calendar,
        UpdateOccurrenceArgs<TData> args)
        where TData : class, ICloneable
    {
        return await calendar.UpdateOccurrenceRangeAsync([args]);
    }

    public static async Task<EventGroup?> EventGroupAsync<TData>(
        this ICalendar<TData> calendar,
        Guid id)
        where TData : class, ICloneable
    {
        var result = await calendar.EventGroupAsync([id]);
        return result.FirstOrDefault();
    }

    public static async Task<Occurrence<TData>?> FirstOrDefaultOccurrenceByEventAsync<TData>(
        this ICalendar<TData> calendar,
        IEnumerable<string> eventIds,
        bool isRespectAdjustments = false)
        where TData : class, ICloneable
    {
        eventIds = eventIds.Distinct().ToArray();
        if (!eventIds.Any()) return null;
        
        var result = await calendar.OccurrencesByEventAsync(
            new OccurrencesByEventQueryArgs(eventIds, count: 1, isRespectAdjustments: isRespectAdjustments));
        
        return result.SelectMany(x => x).MinBy(x => x.Period.Start);
    }
}