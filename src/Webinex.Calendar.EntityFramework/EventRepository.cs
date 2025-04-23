using System.Collections.Concurrent;
using Microsoft.EntityFrameworkCore;
using Webinex.Asky;
using Webinex.Calendar.Common;

namespace Webinex.Calendar.EntityFramework;

internal record EventCacheEntry<TData>(IEventEntityBase Value, IEventRow Row, bool Deleted = false)
    where TData : class, ICloneable;

public class EventRepository<TData> : IEventRepository<TData>
    where TData : class, ICloneable
{
    private readonly ICalendarDbContextProvider<TData> _dbContextProvider;
    private readonly IAskyFieldMap<EventRow<TData>> _eventRowFieldMap;
    private readonly IAskyFieldMap<RecurrentEventRow<TData>> _recurrentEventRowFieldMap;
    private readonly ConcurrentDictionary<string, EventCacheEntry<TData>> _cache = new();

    public EventRepository(
        ICalendarDbContextProvider<TData> dbContextProvider,
        IAskyFieldMap<EventRow<TData>> eventRowFieldMap,
        IAskyFieldMap<RecurrentEventRow<TData>> recurrentEventRowFieldMap)
    {
        _dbContextProvider = dbContextProvider;
        _eventRowFieldMap = eventRowFieldMap;
        _recurrentEventRowFieldMap = recurrentEventRowFieldMap;
    }

    private DbContext DbContext => _dbContextProvider.Value;
    private DbSet<EventRow<TData>> Events => DbContext.Set<EventRow<TData>>();
    private DbSet<RecurrentEventRow<TData>> RecurrentEvents => DbContext.Set<RecurrentEventRow<TData>>();

    public virtual async Task<IReadOnlyCollection<T>> ByIdAsync<T>(IEnumerable<string> ids) where T : IEventEntityBase
    {
        ids = ids?.Distinct().ToArray() ?? throw new ArgumentNullException(nameof(ids));
        var fromCache = ByIdFromCache<T>(ids);
        var fromDatabase = await ByIdFromDatabaseAsync<T>(ids.Except(fromCache.Select(x => x.Id)));
        return fromCache.Concat(fromDatabase).ToArray();
    }

    private IReadOnlyCollection<T> ByIdFromCache<T>(IEnumerable<string> ids)
        where T : IEventEntityBase
    {
        ids = ids?.Distinct().ToArray() ?? throw new ArgumentNullException(nameof(ids));
        return ids.Select(_cache.GetValueOrDefault).Where(x => x != null).Select(x => x!.Value).OfType<T>().ToArray();
    }

    private async Task<IReadOnlyCollection<T>> ByIdFromDatabaseAsync<T>(IEnumerable<string> ids)
    {
        ids = ids?.Distinct().ToArray() ?? throw new ArgumentNullException(nameof(ids));
        var eventIds = ids.Where(EventId.IsOneTime).Concat(ids.Where(OccurrenceId.IsValid)).Distinct().ToArray();
        var recurrentEventIds = ids.Where(EventId.IsRecurrent);

        var eventRows = await Events.Where(x => eventIds.Contains(x.Id)).ToArrayAsync();
        var recurrentEventRows = await RecurrentEvents.Where(x => recurrentEventIds.Contains(x.Id)).ToArrayAsync();
        return eventRows.Select(Instance).Concat(recurrentEventRows.Select(Instance)).OfType<T>().ToArray();
    }

    private IEventEntityBase Instance(IEventRow row)
    {
        return _cache.GetOrAdd(row.Id, _ => new EventCacheEntry<TData>(row.ToEventEntity(), row)).Value;
    }

    public virtual Task<IReadOnlyDictionary<Operation, IEventEntityBase>> PatchAsync(
        IEnumerable<Operation> operations)
    {
        operations = operations?.ToArray() ?? throw new ArgumentNullException(nameof(operations));

        foreach (var operation in operations)
        {
            switch (operation.Type)
            {
                case OperationType.Add:
                {
                    var row = NewRow(operation.Value);
                    var entry = _cache.GetOrAdd(row.Id, _ => new EventCacheEntry<TData>(operation.Value, row));
                    Add(entry.Row);
                    break;
                }

                case OperationType.Update:
                {
                    var cacheEntry = _cache.GetValueOrDefault(operation.Value.Id) ??
                                     throw new InvalidOperationException(
                                         $"EventRow with Id {operation.Value.Id} not found");

                    cacheEntry.Row.Apply(operation.Value);
                    break;
                }

                case OperationType.Remove:
                {
                    var cacheEntry = _cache.GetValueOrDefault(operation.Value.Id)
                                     ?? throw new InvalidOperationException(
                                         $"EventRow with Id {operation.Value.Id} not found");

                    Remove(cacheEntry.Row);
                    _cache[operation.Value.Id] = cacheEntry with { Deleted = true };
                    break;
                }

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        return Task.FromResult<IReadOnlyDictionary<Operation, IEventEntityBase>>(
            operations.ToDictionary(x => x, x => _cache.GetValueOrDefault(x.Value.Id)!.Value));
    }

    private void Add(IEventRow row)
    {
        switch (row)
        {
            case EventRow<TData> eventRow:
                Events.Add(eventRow);
                break;

            case RecurrentEventRow<TData> recurrentEventRow:
                RecurrentEvents.Add(recurrentEventRow);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(row), $"Unexpected type {row.GetType().FullName}");
        }
    }

    private void Remove(IEventRow row)
    {
        switch (row)
        {
            case EventRow<TData> eventRow:
                Events.Remove(eventRow);
                break;

            case RecurrentEventRow<TData> recurrentEventRow:
                RecurrentEvents.Remove(recurrentEventRow);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(row), $"Unexpected type {row.GetType().FullName}");
        }
    }

    private IEventRow NewRow(IEventEntityBase eventEntity)
    {
        return eventEntity switch
        {
            Event<TData> @event when @event.Recurrence != null => RecurrentEventRow<TData>.From(@event),
            _ => EventRow<TData>.From(eventEntity),
        };
    }

    public async Task<IReadOnlyCollection<IEventEntityBase>> MatchAsync(
        Period<DateTimeOffset> period,
        FilterRule? dataFilterRule = null)
    {
        dataFilterRule = dataFilterRule?.Replace(new RenameFieldIdFilterRuleVisitor(x => $"{Event.DATA_FIELD}.{x}"));

        var recurrentEventRows = await MatchRecurrentEventRowsAsync(period, dataFilterRule);
        var eventRows = await MatchEventRowsAsync(period, dataFilterRule, recurrentEventRows);
        recurrentEventRows = await EnsureAllRequiredRecurrentEventRowsAsync(eventRows, recurrentEventRows);
        return eventRows.Select(Instance).Concat(recurrentEventRows.Select(Instance)).ToArray();
    }

    private async Task<IReadOnlyCollection<RecurrentEventRow<TData>>> MatchRecurrentEventRowsAsync(
        Period<DateTimeOffset> period,
        FilterRule? dataFilterRule)
    {
        var queryable = RecurrentEvents
            .Where(x => x.Effective.Start < period.End)
            .Where(x => x.Effective.End == null || x.Effective.End > period.Start);

        if (dataFilterRule != null)
            queryable = queryable.Where(_recurrentEventRowFieldMap, dataFilterRule, FilterOptions.RemoveNotNullChecks);

        return await queryable.ToArrayAsync();
    }

    private async Task<IReadOnlyCollection<EventRow<TData>>> MatchEventRowsAsync(
        Period<DateTimeOffset> period,
        FilterRule? dataFilterRule,
        IReadOnlyCollection<RecurrentEventRow<TData>> recurrentEventRows)
    {
        var queryable = Events
            .Where(x => x.Effective.Start < period.End)
            // optimizes index as explicitly define a range for an effective.start
            // we assume a limitation of max event duration is 24h and max move is 24h
            .Where(x => x.Effective.Start >= period.Start.AddDays(-2))
            .Where(x => x.Effective.End > period.Start);

        if (dataFilterRule != null)
        {
            var filterRule = FilterRule.Or(
                dataFilterRule,
                FilterRule.In("recurrentEventId", recurrentEventRows.Select(x => x.Id).ToArray()));

            queryable = queryable.Where(_eventRowFieldMap, filterRule, FilterOptions.RemoveNotNullChecks);
        }

        return await queryable.ToArrayAsync();
    }

    // This issue may appear when some recurrence exception matched by data or effective period, but
    // it's original recurrent event doesn't
    private async Task<IReadOnlyCollection<RecurrentEventRow<TData>>> EnsureAllRequiredRecurrentEventRowsAsync(
        IReadOnlyCollection<EventRow<TData>> eventRows,
        IReadOnlyCollection<RecurrentEventRow<TData>> recurrentEventRows)
    {
        var requiredRecurrentEventIds = eventRows.Where(x => x.RecurrentEventId != null).Select(x => x.RecurrentEventId)
            .Distinct().ToArray();
        var existingRecurrentEventIds = recurrentEventRows.Select(x => x.Id).ToArray();
        var missedRecurrentEventIds = requiredRecurrentEventIds.Except(existingRecurrentEventIds).ToArray();
        var missedRecurrentEventRows =
            await RecurrentEvents.Where(x => missedRecurrentEventIds.Contains(x.Id)).ToArrayAsync();
        return recurrentEventRows.Concat(missedRecurrentEventRows).ToArray();
    }

    public virtual async Task<IReadOnlyCollection<T>> GetAllAsync<T>(
        FilterRule? filterRule = null,
        IEnumerable<SortRule>? sortRules = null,
        PagingRule? pagingRule = null,
        bool readOnly = false) where T : IEventEntityBase
    {
        sortRules = sortRules?.ToArray();

        var eventRowResult =
            await Queryable<EventRow<TData>>(MergeTypeFilterRule<T>(filterRule), sortRules, pagingRule, readOnly)
                .ToArrayAsync();

        var events = readOnly
            ? eventRowResult.Select(x => x.ToEventEntity()).ToArray()
            : eventRowResult.Select(Instance).ToArray();

        if (!CanBeRecurrentEvent<T>())
            return events.OfType<T>().ToArray();

        var recurrentEventRowResult = CanBeRecurrentEvent<T>()
            ? await Queryable<RecurrentEventRow<TData>>(filterRule, sortRules, pagingRule, readOnly).ToArrayAsync()
            : [];

        return recurrentEventRowResult.Select(x => readOnly ? x.ToEvent() : Instance(x)).Concat(events).OfType<T>()
            .ToArray();
    }

    public virtual async Task<bool> AnyAsync<T>(FilterRule? filterRule = null) where T : IEventEntityBase
    {
        var eventRowResult = await Queryable<EventRow<TData>>(MergeTypeFilterRule<T>(filterRule)).AnyAsync();
        return eventRowResult || (CanBeRecurrentEvent<T>() &&
                                  await Queryable<RecurrentEventRow<TData>>(filterRule).AnyAsync());
    }

    public virtual async Task<int> CountAsync<T>(FilterRule? filterRule = null) where T : IEventEntityBase
    {
        var eventRowCount = await CountInternalAsync<EventRow<TData>>(MergeTypeFilterRule<T>(filterRule));
        return CanBeRecurrentEvent<T>()
            ? eventRowCount + await CountInternalAsync<RecurrentEventRow<TData>>(filterRule)
            : eventRowCount;
    }

    private async Task<int> CountInternalAsync<TRow>(FilterRule? filterRule) where TRow : class, IEventRow
    {
        return await Queryable<TRow>(filterRule).CountAsync();
    }

    // private IQueryable<IEventRow> Queryable<T>(
    //     FilterRule? filterRule,
    //     IEnumerable<SortRule>? sortRules = null,
    //     PagingRule? pagingRule = null)
    //     where T : IEventEntityBase
    // {
    //     sortRules = sortRules?.ToArray();
    //     var queryable = Queryable(Events, _eventRowFieldMap, MergeTypeFilterRule<T>(filterRule), sortRules, pagingRule);
    //
    //     if (typeof(T) == typeof(IEventEntityBase) || typeof(T) == typeof(Event<TData>))
    //         queryable = queryable.Union(
    //             Queryable(RecurrentEvents, _recurrentEventRowFieldMap, filterRule, sortRules, pagingRule));
    //     
    //     return queryable;
    // }

    private IQueryable<TRow> Queryable<TRow>(
        FilterRule? filterRule,
        IEnumerable<SortRule>? sortRules = null,
        PagingRule? pagingRule = null,
        bool readOnly = false)
        where TRow : class, IEventRow
    {
        sortRules = sortRules?.ToArray();
        var (queryable, fieldMap) = ResolveSource<TRow>();
        if (filterRule != null) queryable = queryable.Where(fieldMap, filterRule);
        if (sortRules?.Any() == true) queryable = queryable.SortBy(fieldMap, sortRules);
        if (pagingRule != null) queryable = queryable.PageBy(pagingRule);
        if (readOnly) queryable = queryable.AsNoTracking();
        return queryable;
    }

    private (IQueryable<TRow> queryable, IAskyFieldMap<TRow> fieldMap) ResolveSource<TRow>()
    {
        if (typeof(TRow) == typeof(EventRow<TData>))
            return ((IQueryable<TRow>)Events.AsQueryable(), (IAskyFieldMap<TRow>)_eventRowFieldMap);
        if (typeof(TRow) == typeof(RecurrentEventRow<TData>))
            return ((IQueryable<TRow>)RecurrentEvents.AsQueryable(), (IAskyFieldMap<TRow>)_recurrentEventRowFieldMap);
        throw new InvalidOperationException($"Unsupported type {typeof(TRow).Name}");
    }

    private FilterRule? MergeTypeFilterRule<T>(FilterRule? filterRule)
        where T : IEventEntityBase
    {
        var type = EventRowType<T>();
        var typeFilterRule = type.HasValue ? FilterRule.Eq("type", type.Value) : null;
        return typeFilterRule == null ? filterRule :
            filterRule != null ? FilterRule.And(typeFilterRule, filterRule) : typeFilterRule;
    }

    private EventRowType? EventRowType<T>()
        where T : IEventEntityBase
    {
        if (typeof(T) == typeof(IEventEntityBase))
            return null;

        if (typeof(T).IsGenericType && typeof(T).GetGenericTypeDefinition() == typeof(Event<>))
            return EntityFramework.EventRowType.Event;

        if (typeof(T).IsGenericType && typeof(T).GetGenericTypeDefinition() == typeof(OccurrenceAdjustment<>))
            return EntityFramework.EventRowType.Occurrence;

        throw new InvalidOperationException(
            $"Type {typeof(T).FullName} is unknown by {nameof(EventRepository<TData>)}");
    }

    private bool CanBeRecurrentEvent<T>()
    {
        return typeof(Event<TData>).IsAssignableTo(typeof(T));
    }
}