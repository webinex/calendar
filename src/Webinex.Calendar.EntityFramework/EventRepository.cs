using System.Collections.Concurrent;
using Microsoft.EntityFrameworkCore;
using Webinex.Asky;
using Webinex.Calendar.Common;
using Webinex.Coded;

namespace Webinex.Calendar.EntityFramework;

public class EventRepository<TData> : IEventRepository<TData>
    where TData : class, ICloneable
{
    private readonly ICalendarDbContextProvider<TData> _dbContextProvider;
    private readonly IAskyFieldMap<EventRow<TData>> _eventRowFieldMap;
    private readonly IAskyFieldMap<RecurrentEventRow<TData>> _recurrentEventRowFieldMap;
    private readonly ConcurrentDictionary<string, IEventEntityBase> _identityMap = new();

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
        var rows = await FindRowsAsync(ids);
        return rows.Select(Entity).OfType<T>().ToArray();
    }

    private async Task<IReadOnlyCollection<IEventRow>> FindRowsAsync(IEnumerable<string> ids)
    {
        ids = ids?.Distinct().ToArray() ?? throw new ArgumentNullException(nameof(ids));
        var eventRowIds = ids.Where(EventId.IsOneTime).Concat(ids.Where(OccurrenceId.IsValid)).ToArray();
        var recurrentEventRowIds = ids.Where(EventId.IsRecurrent).ToList();

        var eventRows = await FindConcreteRowsAsync<EventRow<TData>>(eventRowIds);
        var recurrentEventRows = await FindConcreteRowsAsync<RecurrentEventRow<TData>>(recurrentEventRowIds);
        return eventRows.Cast<IEventRow>().Concat(recurrentEventRows).ToArray();
    }

    private async Task<IEventRow?> FindRowAsync(string id)
    {
        var result = await FindRowsAsync([id]);
        return result.FirstOrDefault();
    }

    private async Task<IReadOnlyCollection<T>> FindConcreteRowsAsync<T>(IEnumerable<string> ids)
        where T : class, IEventRow
    {
        if (typeof(T) == typeof(IEventRow))
            throw new InvalidOperationException($"Might be concrete row type, not a {nameof(IEventRow)} type");

        ids = ids.Distinct().ToArray();
        if (!ids.Any())
            return [];

        var local = DbContext.Set<T>().Local.Where(x => ids.Contains(x.Id)).ToArray();
        var idSurplus = ids.Except(local.Select(x => x.Id)).ToArray();
        if (idSurplus.Length == 0)
            return local;

        var db = await DbContext.Set<T>().Where(x => ((IEnumerable<string>)idSurplus).Contains(x.Id)).ToArrayAsync();
        return local.Concat(db).ToArray();
    }

    private IEventEntityBase Entity(IEventRow row)
    {
        if (_identityMap.TryGetValue(row.Id, out var result))
            return result;

        result = row.ToEventEntity();
        _identityMap[row.Id] = result;
        return result;
    }

    public virtual async Task<IReadOnlyDictionary<Operation, IEventEntityBase>> PatchAsync(
        IEnumerable<Operation> operations)
    {
        operations = operations?.ToArray() ?? throw new ArgumentNullException(nameof(operations));
        
        // preload all rows to DbContext cache
        await FindRowsAsync(operations
            .Where(x => x.Type is OperationType.Update or OperationType.Remove).Select(x => x.Value.Id));

        foreach (var operation in operations)
        {
            switch (operation.Type)
            {
                case OperationType.Add:
                {
                    var row = NewRow(operation.Value);
                    _identityMap[row.Id] = operation.Value;
                    Add(row);
                    break;
                }

                case OperationType.Update:
                {
                    var row = await FindRowAsync(operation.Value.Id)
                              ?? throw CodedException.NotFound(operation.Value.Id);
                    row.Apply(operation.Value);
                    _identityMap[operation.Value.Id] = operation.Value;
                    break;
                }

                case OperationType.Remove:
                {
                    var row = await FindRowAsync(operation.Value.Id)
                        ?? throw CodedException.NotFound(operation.Value.Id);
                    Remove(row);
                    _identityMap.TryRemove(operation.Value.Id, out _);
                    break;
                }

                default:
                    throw new ArgumentOutOfRangeException($"Unexpected type {operation.Type}");
            }
        }

        return operations.ToDictionary(x => x, x => x.Value);
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
        return eventRows.Cast<IEventRow>().Concat(recurrentEventRows).Select(Entity).ToArray();
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
            // we assume a limitation of max event duration is 24h and max move is 72h
            .Where(x => x.Effective.Start >= period.Start.AddDays(-4))
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
        var missedRecurrentEventIds = requiredRecurrentEventIds.Except(existingRecurrentEventIds).ToList();
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
            : eventRowResult.Select(Entity).ToArray();

        if (!CanBeRecurrentEvent<T>())
            return events.OfType<T>().ToArray();

        var recurrentEventRowResult = CanBeRecurrentEvent<T>()
            ? await Queryable<RecurrentEventRow<TData>>(filterRule, sortRules, pagingRule, readOnly).ToArrayAsync()
            : [];

        var recurrentEvents = readOnly
            ? recurrentEventRowResult.Select(x => x.ToEvent()).ToArray()
            : recurrentEventRowResult.Select(Entity).ToArray();

        return recurrentEvents.Concat(events).OfType<T>()
            .ToArray();
    }

    public async Task<IReadOnlyCollection<IEventEntityBase>> GetAllAsync(
        EventEntityType type,
        FilterRule? filterRule = null,
        IEnumerable<SortRule>? sortRules = null,
        PagingRule? pagingRule = null,
        bool readOnly = false)
    {
        sortRules = sortRules?.ToArray();
        var result = new List<IEventEntityBase>();

        if (type.HasFlag(EventEntityType.OneTimeEvent) || type.HasFlag(EventEntityType.OccurrenceAdjustment))
        {
            var eventRows = await Queryable<EventRow<TData>>(filterRule, sortRules, pagingRule, readOnly)
                .ToArrayAsync();

            var events = readOnly
                ? eventRows.Select(x => x.ToEventEntity()).ToArray()
                : eventRows.Select(Entity).ToArray();

            result.AddRange(events);
        }

        if (type.HasFlag(EventEntityType.RecurrentEvent))
        {
            var recurrentEventRows =
                await Queryable<RecurrentEventRow<TData>>(filterRule, sortRules, pagingRule, readOnly).ToArrayAsync();
            var recurrentEvents = readOnly
                ? recurrentEventRows.Select(x => x.ToEvent()).ToArray()
                : recurrentEventRows.Select(Entity).ToArray();
            result.AddRange(recurrentEvents);
        }

        return result.ToArray();
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

    public async Task<IReadOnlyCollection<EventGroup>> EventGroupAsync(IEnumerable<Guid> ids)
    {
        ids = ids?.Distinct().ToArray() ?? throw new ArgumentNullException(nameof(ids));
        if (!ids.Any()) return [];

        var result = await DbContext.Set<RecurrentEventRow<TData>>()
            .Where(x => ids.Contains(x.Group.Id))
            .GroupBy(x => x.Group.Id)
            .Select(x => new
            {
                Id = x.Key,
                Start = x.Min(e => e.Recurrence.MGRecurrence!.Period.Start),
                End = x.Max(e => e.Recurrence.MGRecurrence!.Period.End ?? DateOnly.MaxValue),
            })
            .ToArrayAsync();

        return result.Select(x => new EventGroup(x.Id, x.Start, x.End)).ToArray();
    }

    private async Task<int> CountInternalAsync<TRow>(FilterRule? filterRule) where TRow : class, IEventRow
    {
        return await Queryable<TRow>(filterRule).CountAsync();
    }

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