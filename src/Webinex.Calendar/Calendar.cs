using Microsoft.EntityFrameworkCore;
using Webinex.Asky;
using Webinex.Calendar.Caches;
using Webinex.Calendar.Common;
using Webinex.Calendar.DataAccess;
using Webinex.Calendar.Events;
using Webinex.Calendar.Filters;
using Webinex.Coded;

namespace Webinex.Calendar;

internal class Calendar<TData> : ICalendar<TData>, IOneTimeEventCalendarInstance<TData>,
    IRecurrentEventCalendarInstance<TData>
    where TData : class, ICloneable
{
    private readonly EfLocalCache<TData> _localCache;
    private readonly ICalendarDbContext<TData> _dbContext;
    private readonly IAskyFieldMap<TData> _dataFieldMap;
    private readonly IRecurrentEventRowAskyFieldMap<TData> _recurrentEventRowAskyFieldMap;
    private readonly IRecurrentEventStateAskyFieldMap<TData> _recurrentEventStateAskyFieldMap;
    private readonly ICache<TData> _cache;
    private readonly ICalendarSettings<TData> _settings;

    public Calendar(
        ICalendarDbContext<TData> dbContext,
        IAskyFieldMap<TData> dataFieldMap,
        IRecurrentEventRowAskyFieldMap<TData> recurrentEventRowAskyFieldMap,
        IRecurrentEventStateAskyFieldMap<TData> recurrentEventStateAskyFieldMap,
        ICache<TData> cache,
        ICalendarSettings<TData> settings)
    {
        _dbContext = dbContext;
        _dataFieldMap = dataFieldMap;
        _recurrentEventRowAskyFieldMap = recurrentEventRowAskyFieldMap;
        _recurrentEventStateAskyFieldMap = recurrentEventStateAskyFieldMap;
        _cache = cache;
        _settings = settings;
        _localCache = new EfLocalCache<TData>(dbContext.Events.Local);
    }

    public IOneTimeEventCalendarInstance<TData> OneTime => this;
    public IRecurrentEventCalendarInstance<TData> Recurrent => this;

    async Task<OneTimeEvent<TData>?> IOneTimeEventCalendarInstance<TData>.GetAsync(Guid id)
    {
        var row = await FindAsync(id);
        return row?.ToOneTimeEvent();
    }

    async Task<OneTimeEvent<TData>[]> IOneTimeEventCalendarInstance<TData>.GetManyAsync(IEnumerable<Guid> ids)
    {
        var result = await GetManyAsync(ids);
        return result.Select(x => x.ToOneTimeEvent()).ToArray();
    }

    async Task<OneTimeEvent<TData>> IOneTimeEventCalendarInstance<TData>.AddAsync(OneTimeEvent<TData> @event)
    {
        @event = @event ?? throw new ArgumentNullException(nameof(@event));
        var row = EventRow<TData>.NewEvent(@event.Id, @event.Period, @event.Data);
        await _dbContext.Events.AddAsync(row);
        _cache.PushAdd(row);
        return row.ToOneTimeEvent();
    }

    async Task<OneTimeEvent<TData>> IOneTimeEventCalendarInstance<TData>.UpdateDataAsync(
        OneTimeEvent<TData> @event,
        TData data)
    {
        var row = await FindRequiredAsync(@event.Id);
        row.SetOneTimeEventData(data);
        _cache.PushUpdate(row);
        return row.ToOneTimeEvent();
    }

    async Task IOneTimeEventCalendarInstance<TData>.DeleteAsync(OneTimeEvent<TData> @event)
    {
        var row = await FindRequiredAsync(@event.Id);
        _dbContext.Events.Remove(row);
        _cache.PushDelete(row);
    }

    async Task IOneTimeEventCalendarInstance<TData>.CancelAsync(OneTimeEvent<TData> @event)
    {
        var row = await FindRequiredAsync(@event.Id, EventType.RecurrentEvent);
        row.Cancel();
        _cache.PushUpdate(row);
    }

    async Task<RecurrentEvent<TData>?> IRecurrentEventCalendarInstance<TData>.GetAsync(Guid id)
    {
        var row = await FindAsync(id, EventType.RecurrentEvent);
        return row?.ToRecurrentEvent();
    }

    async Task<RecurrentEvent<TData>[]> IRecurrentEventCalendarInstance<TData>.GetManyAsync(IEnumerable<Guid> ids)
    {
        var result = await GetManyAsync(ids);
        return result.Select(x => x.ToRecurrentEvent()).ToArray();
    }

    async Task<RecurrentEvent<TData>[]> IRecurrentEventCalendarInstance<TData>.GetAllAsync(FilterRule filter)
    {
        filter = filter.Replace(new RecurrentEventFilterRuleVisitor());

        var result = await FilterWithLocalChanges(q => q
            .Where(x => x.Type == EventType.RecurrentEvent)
            .Where(_recurrentEventRowAskyFieldMap, filter));

        return result.Select(x => x.ToRecurrentEvent()).ToArray();
    }

    async Task<RecurrentEvent<TData>[]> IRecurrentEventCalendarInstance<TData>.GetManyAsync(
        FilterRule filter,
        SortRule? sortRule,
        PagingRule? pagingRule)
    {
        filter = filter.Replace(new RecurrentEventFilterRuleVisitor());

        var queryable = _dbContext.Events
            .Where(x => x.Type == EventType.RecurrentEvent)
            .Where(_recurrentEventRowAskyFieldMap, filter);

        if (sortRule != null)
            queryable = queryable.SortBy(_recurrentEventRowAskyFieldMap, sortRule);

        if (pagingRule != null)
            queryable = queryable.PageBy(pagingRule);

        var result = await queryable.ToArrayAsync();
        return result.Select(x => x.ToRecurrentEvent()).ToArray();
    }

    async Task<Event<TData>> IRecurrentEventCalendarInstance<TData>.SaveDataAsync(
        RecurrentEvent<TData> @event,
        DateTimeOffset eventStart,
        TData data)
    {
        @event = @event ?? throw new ArgumentNullException(nameof(@event));
        data = data ?? throw new ArgumentNullException(nameof(data));
        AssertEventMightExist(@event, eventStart);

        var stateRow = await FindRecurrentEventStateAsync(@event.Id, eventStart);

        if (stateRow == null)
        {
            var eventEnd = eventStart.AddMinutes(@event.DurationMinutes());
            stateRow = EventRow<TData>.NewRecurrentEventState(@event.Id, eventStart, eventEnd, data, null, false);
            await _dbContext.Events.AddAsync(stateRow);
            _cache.PushAdd(stateRow);
        }
        else
        {
            stateRow.SetRecurrentEventData(data);
            _cache.PushUpdate(stateRow);
        }

        return new Event<TData>(null, @event.Id, stateRow.Effective.ToPeriod(), data, false,
            stateRow.MoveTo != null ? stateRow.Effective.ToPeriod() : null);
    }

    async Task<Event<TData>> IRecurrentEventCalendarInstance<TData>.AddDataAsync(
        RecurrentEvent<TData> @event,
        DateTimeOffset eventStart,
        TData data)
    {
        @event = @event ?? throw new ArgumentNullException(nameof(@event));
        data = data ?? throw new ArgumentNullException(nameof(data));
        AssertEventMightExist(@event, eventStart);

        var dataRow =
            EventRow<TData>.NewRecurrentEventState(@event.Id, eventStart,
                eventStart.AddMinutes(@event.DurationMinutes()), data,
                null, false);
        await _dbContext.Events.AddAsync(dataRow);
        _cache.PushAdd(dataRow);

        return new Event<TData>(null, @event.Id, dataRow.Effective.ToPeriod(), data, false,
            dataRow.MoveTo != null ? dataRow.Effective.ToPeriod() : null);
    }

    async Task<Event<TData>> IRecurrentEventCalendarInstance<TData>.UpdateDataAsync(
        RecurrentEvent<TData> @event,
        DateTimeOffset eventStart,
        TData data)
    {
        @event = @event ?? throw new ArgumentNullException(nameof(@event));
        data = data ?? throw new ArgumentNullException(nameof(data));
        AssertEventMightExist(@event, eventStart);

        var dataRow = await FindRequiredRecurrentEventStateAsync(@event.Id, eventStart);
        dataRow.SetRecurrentEventData(data);
        _cache.PushUpdate(dataRow);

        return new Event<TData>(null, @event.Id, dataRow.Effective.ToPeriod(), data, false,
            dataRow.MoveTo != null ? dataRow.Effective.ToPeriod() : null);
    }

    async Task IRecurrentEventCalendarInstance<TData>.DeleteStateAsync(RecurrentEventStateId id)
    {
        var state = await FindRequiredRecurrentEventStateAsync(id.RecurrentEventId, id.EventStart);
        _dbContext.Events.Remove(state);
        _cache.PushDelete(state);
    }

    async Task<RecurrentEvent<TData>> IRecurrentEventCalendarInstance<TData>.AddAsync(RecurrentEvent<TData> @event)
    {
        @event = @event ?? throw new ArgumentNullException(nameof(@event));

        if (@event.Repeat.TimeZone() != null && !@event.Repeat.TimeZone()!.Equals(_settings.TimeZone))
            throw new InvalidOperationException("TimeZone might match .UseTimeZone() specified for calendar.");

        var row = EventRow<TData>.NewRecurrentEvent(@event.Id, @event.Repeat, @event.Effective,
            (TData)@event.Data.Clone());
        await _dbContext.Events.AddAsync(row);
        _cache.PushAdd(row);
        return @event;
    }

    async Task<RecurrentEvent<TData>[]> IRecurrentEventCalendarInstance<TData>.AddRangeAsync(
        IEnumerable<RecurrentEvent<TData>> events)
    {
        var result = new LinkedList<RecurrentEvent<TData>>();
        foreach (var @event in events)
            result.AddLast(await Recurrent.AddAsync(@event));

        return result.ToArray();
    }

    async Task IRecurrentEventCalendarInstance<TData>.MoveAsync(
        RecurrentEvent<TData> @event,
        DateTimeOffset eventStart,
        Period moveTo)
    {
        @event = @event ?? throw new ArgumentNullException(nameof(@event));
        moveTo = moveTo ?? throw new ArgumentNullException(nameof(moveTo));
        AssertEventMightExist(@event, eventStart);

        var stateRow = await FindRecurrentEventStateAsync(@event.Id, eventStart);

        if (stateRow == null)
        {
            var eventEnd = eventStart.AddMinutes(@event.DurationMinutes());
            stateRow = EventRow<TData>.NewRecurrentEventState(@event.Id, eventStart, eventEnd,
                (TData)@event.Data.Clone(), moveTo, false);
            await _dbContext.Events.AddAsync(stateRow);
            _cache.PushAdd(stateRow);
        }
        else
        {
            stateRow.Move(moveTo);
            _cache.PushUpdate(stateRow);
        }
    }

    async Task IRecurrentEventCalendarInstance<TData>.CancelAsync(Guid id, DateTimeOffset since)
    {
        var row = await FindRequiredAsync(id, EventType.RecurrentEvent);

        if (row.Effective.End < since.TotalMinutesSince1990())
            throw CodedException.Invalid("Should be less than current end");

        row.Effective = new OpenPeriodMinutesSince1990(row.Effective.Start, since.TotalMinutesSince1990());
        _cache.PushUpdate(row);

        var states = await FilterWithLocalChanges(q => q
            .Where(x => x.Type == EventType.RecurrentEventState && x.RecurrentEventId == id)
            .Where(x => x.Effective.Start >= since.TotalMinutesSince1990() || (x.MoveTo != null && x.MoveTo.Start >= since)));
        states = states.ToArray();

        var remove = states.Where(x =>
                x.Effective.Start >= since.TotalMinutesSince1990() &&
                (x.MoveTo?.Start == null || x.MoveTo.Start > since))
            .ToArray();

        var cancel = states.Except(remove).ToArray();
        foreach (var rowToCancel in cancel)
        {
            rowToCancel.Cancel();
        }

        _cache.PushUpdate(cancel);

        _dbContext.Events.RemoveRange(remove);
        _cache.PushDelete(remove);
        return;
    }

    async Task IRecurrentEventCalendarInstance<TData>.CancelAppearanceAsync(
        RecurrentEvent<TData> @event,
        DateTimeOffset eventStart)
    {
        @event = @event ?? throw new ArgumentNullException(nameof(@event));
        AssertEventMightExist(@event, eventStart);

        var stateRow = await FindRecurrentEventStateAsync(@event.Id, eventStart);

        if (stateRow == null)
        {
            var eventEnd = eventStart.AddMinutes(@event.DurationMinutes());
            stateRow = EventRow<TData>.NewRecurrentEventState(@event.Id, eventStart, eventEnd,
                (TData)@event.Data.Clone(), null, true);
            await _dbContext.Events.AddAsync(stateRow);
            _cache.PushAdd(stateRow);
        }
        else
        {
            stateRow.Cancel();
            _cache.PushUpdate(stateRow);
        }
    }

    async Task IRecurrentEventCalendarInstance<TData>.DeleteAsync(Guid recurrentEventId)
    {
        var rows = await FilterWithLocalChanges(q =>
            q.Where(x => (x.Type == EventType.RecurrentEvent && x.Id == recurrentEventId) ||
                         (x.Type == EventType.RecurrentEventState && x.RecurrentEventId == recurrentEventId)));
        rows = rows.ToArray();

        var recurrentEvent = rows.FirstOrDefault(x => x.Id == recurrentEventId)?.ToRecurrentEvent();
        if (recurrentEvent == null)
            throw CodedException.NotFound(recurrentEventId);

        _dbContext.Events.RemoveRange(rows);
        _cache.PushDelete(rows);
    }

    Task IRecurrentEventCalendarInstance<TData>.DeleteAsync(RecurrentEvent<TData> @event)
    {
        return Recurrent.DeleteAsync(@event.Id);
    }

    async Task IRecurrentEventCalendarInstance<TData>.DeleteRangeAsync(IEnumerable<RecurrentEvent<TData>> events)
    {
        // TODO: s.skalaban, avoid cycle
        foreach (var @event in events)
            await Recurrent.DeleteAsync(@event.Id);
    }

    async Task<RecurrentEventState<TData>?> IRecurrentEventCalendarInstance<TData>.GetStateAsync(
        RecurrentEventStateId id)
    {
        var result = await FindRecurrentEventStateAsync(id.RecurrentEventId, id.EventStart);
        return result?.ToRecurrentEventState();
    }

    async Task<RecurrentEventState<TData>[]> IRecurrentEventCalendarInstance<TData>.GetAllStatesAsync(FilterRule filter)
    {
        filter = filter.Replace(
            new DateTimeOffsetToMinutesSince1990FilterRuleVisitor(new[] { "period.start", "period.end" }));

        var result = await FilterWithLocalChanges(q => q
            .Where(_recurrentEventStateAskyFieldMap, filter)
            .Where(x => x.Type == EventType.RecurrentEventState)
            .Where(e => !e.Cancelled));

        return result.Select(x => x.ToRecurrentEventState()).ToArray();
    }

    async Task<RecurrentEventState<TData>[]> IRecurrentEventCalendarInstance<TData>.GetManyStatesAsync(
        IEnumerable<RecurrentEventStateId> ids)
    {
        ids = ids.Distinct().ToArray();

        var result = await FilterWithLocalChanges(
            e => new RecurrentEventStateId(e.RecurrentEventId!.Value, Constants.J1_1990.AddMinutes(e.Effective.Start)),
            e => FilterRule.And(
                FilterRule.Eq("effective.start", e.EventStart.TotalMinutesSince1990()),
                FilterRule.Eq("recurrentEventId", e.RecurrentEventId)),
            ids);

        return result.Select(x => x.ToRecurrentEventState()).ToArray();
    }

    public async Task<Event<TData>[]> GetCalculatedAsync(
        DateTimeOffset from,
        DateTimeOffset to,
        FilterRule? dataFilterRule = null,
        QueryOptions queryOptions = QueryOptions.Db,
        DbFilterOptimization? filterOptimization = default)
    {
        var period = new Period(from, to);
        var dataFilter = dataFilterRule != null ? AskyExpressionFactory.Create(_dataFieldMap, dataFilterRule) : null;

        // +/- 1 hour needed to ensure DST transition events loaded, it would be additionally filtered at the end of method
        var rows = await GetRowsFromCacheOrFallbackToDbContextAsync(from.AddHours(-1), to.AddHours(1), dataFilterRule,
            queryOptions);
        var oneTimeEvents = rows.Where(x => x.Type == EventType.OneTimeEvent).ToArray();

        var recurrentEventStates = rows
            .Where(x => x.Type == EventType.RecurrentEventState)
            .Select(x => x.ToRecurrentEventState()).ToArray();

        var recurrentEventStateById = recurrentEventStates.ToLookup(x => x.RecurrentEventId);

        var recurrentEvents = rows.Where(x => x.Type == EventType.RecurrentEvent).ToArray();
        var statesWithoutRecurrentEvent = recurrentEventStates
            .Where(s => recurrentEvents.All(r => r.Id != s.RecurrentEventId)).ToArray();

        var result = recurrentEvents
            .SelectMany(ev => ev.ToRecurrentEvent().ToEvents(from, to, recurrentEventStateById[ev.Id]))
            .Concat(oneTimeEvents.Select(ev => ev.ToOneTimeEvent().ToEvent()))
            .Concat(statesWithoutRecurrentEvent.Select(x => x.ToEvent()))
            .ToArray();

        result = result.Where(x => !x.Cancelled).ToArray();

        // We have to filter again, because result might contain unfiltered data.
        // Example: we want to get events, where Data == "4"
        // in result we have RecurrentEvent with Data == "4" and RecurrentEventState with Data == "1"
        if (dataFilter != null)
            result = result.Where(Expressions.Child<Event<TData>, TData>(x => x.Data, dataFilter).Compile()).ToArray();

        return result.Where(x => period.Intersects(new Period(x.Start, x.End))).ToArray();
    }

    private async Task<EventRow<TData>[]> GetRowsFromCacheOrFallbackToDbContextAsync(
        DateTimeOffset from,
        DateTimeOffset to,
        FilterRule? dataFilterRule = null,
        QueryOptions queryOptions = QueryOptions.Db,
        DbFilterOptimization? filterOptimization = default)
    {
        if (queryOptions == QueryOptions.TryCache &&
            _cache.TryGetAll(from, to, dataFilterRule, out var rows))
            return rows!.Value.ToArray();

        return await GetRowsFromDbContextAsync(from, to, dataFilterRule, filterOptimization);
    }

    private async Task<EventRow<TData>[]> GetRowsFromDbContextAsync(
        DateTimeOffset from,
        DateTimeOffset to,
        FilterRule? dataFilterRule = null,
        DbFilterOptimization? filterOptimization = default)
    {
        var dataFilter = dataFilterRule != null ? AskyExpressionFactory.Create(_dataFieldMap, dataFilterRule) : null;
        var filters = new DbQuery<TData>(from, to, dataFilter, _settings.TimeZone,
            filterOptimization ?? _settings.DbQueryOptimization);

        var local = filters.ToArray(await GetLocalRows());
        var db = await filters.ToArrayAsync(_dbContext.Events.AsQueryable());

        return FilterOutLocallyDeletedRows(local.Concat(db.Except(local))).ToArray();

        // We can't use _dbContext.Events.Local directly, because we have filtering by eventRow.RecurrentEvent Navigation in DbQuery
        // In this prop we store RecurrentEvent of RecurrentEventState. When we do filtering in db ef convert predicate to sql with join,
        // but then if AutoInclude is false we get states with RecurrentEvent == null. To filter properly in memory we have to manually
        // fill this data
        async Task<IEnumerable<EventRow<TData>>> GetLocalRows()
        {
            var localRows = _localCache.AsEnumerable().ToList();
            if (!localRows.Any())
                return localRows;

            var localStatesWithoutRecurrentEvent = localRows
                .Where(e => e.IsRecurrentEventState())
                .Where(e => e.RecurrentEvent == null)
                .ToArray();

            if (!localStatesWithoutRecurrentEvent.Any())
                return localRows;

            var recurrentEventIds = localStatesWithoutRecurrentEvent.Select(e => e.RecurrentEventId!.Value).ToArray();

            var recurrentEvents = _localCache.AsEnumerable()
                .Where(e => e.Type == EventType.RecurrentEvent)
                .Where(e => recurrentEventIds.Contains(e.Id))
                .ToDictionary(e => e.Id);

            var notFoundRecurrentEventsIds = recurrentEventIds.Except(recurrentEvents.Keys).ToArray();
            // we didn't find all recurrent events in local, so we have to query data from db
            if (notFoundRecurrentEventsIds.Any())
            {
                var dbRecurrentEvents = await _dbContext.Events
                    .Where(e => notFoundRecurrentEventsIds.Contains(e.Id))
                    .ToDictionaryAsync(e => e.Id);
                recurrentEvents = recurrentEvents.Concat(dbRecurrentEvents).ToDictionary(e => e.Key, e => e.Value);
            }

            foreach (var eventRow in localStatesWithoutRecurrentEvent)
            {
                if (recurrentEvents.TryGetValue(eventRow.RecurrentEventId!.Value, out var recurrentEvent))
                {
                    eventRow.RecurrentEvent = recurrentEvent;
                }
                else
                {
                    // For some reasons we didn't find recurrent event in db and in local.
                    // We assume this is because recurrentEvent was deleted, so we skip this row
                    localRows.Remove(eventRow);
                }
            }

            return localRows;
        }
    }

    private async Task<EventRow<TData>?> FindAsync(Guid id, EventType? type = null)
    {
        var eventRow = await _dbContext.Events.FindAsync(id);
        if (type.HasValue && eventRow != null && eventRow.Type != type)
        {
            throw new InvalidOperationException(
                $"{nameof(EventRow<ICloneable>)} {id} type {eventRow.Type:G} doesn't match required type {type:G}");
        }

        return eventRow;
    }

    private async Task<EventRow<TData>> FindRequiredAsync(Guid id, EventType? type = null)
    {
        return await FindAsync(id, type) ?? throw CodedException.NotFound(id);
    }

    private async Task<EventRow<TData>?> FindRecurrentEventStateAsync(Guid recurrentEventId, DateTimeOffset dateTime)
    {
        var result = await FilterWithLocalChanges(
            e => new RecurrentEventStateId(e.RecurrentEventId!.Value, Constants.J1_1990.AddMinutes(e.Effective.Start)),
            e => FilterRule.And(
                FilterRule.Eq("effective.start", e.EventStart.TotalMinutesSince1990()),
                FilterRule.Eq("recurrentEventId", e.RecurrentEventId)),
            new[] { new RecurrentEventStateId(recurrentEventId, dateTime) });

        return result.FirstOrDefault();
    }

    private async Task<EventRow<TData>> FindRequiredRecurrentEventStateAsync(Guid id, DateTimeOffset dateTime)
    {
        return await FindRecurrentEventStateAsync(id, dateTime) ?? throw CodedException.NotFound(new { id, dateTime });
    }

    private async Task<EventRow<TData>[]> GetManyAsync(IEnumerable<Guid> ids)
    {
        var result = await FilterWithLocalChanges(e => e.Id, e => FilterRule.Eq("id", e), ids);
        return result.ToArray();
    }

    private async Task<IEnumerable<EventRow<TData>>> FilterWithLocalChanges(
        Func<IQueryable<EventRow<TData>>, IQueryable<EventRow<TData>>> filter)
    {
        var localStates = filter(_localCache.AsQueryable()).ToArray();
        var dbStates = await filter(_dbContext.Events).ToArrayAsync();

        // We need to filter again, because of local changes
        return filter(FilterOutLocallyDeletedRows(localStates.Concat(dbStates.Except(localStates))).AsQueryable());
    }

    private async Task<IEnumerable<EventRow<TData>>> FilterWithLocalChanges<TId>(
        Func<EventRow<TData>, TId> idAccessor,
        Func<TId, FilterRule> idToFilterRuleConverter,
        IEnumerable<TId> ids) where TId : notnull
    {
        ids = ids.Distinct().ToArray();

        if (!ids.Any())
            return Enumerable.Empty<EventRow<TData>>();

        var idsFilters = ids.ToDictionary(e => e, idToFilterRuleConverter);

        var localStates = _localCache.AsQueryable()
            .Where(_recurrentEventRowAskyFieldMap,
                idsFilters.Values.Count == 1 ? idsFilters.Values.First() : FilterRule.Or(idsFilters.Values))
            .ToArray();
        var surplusIds = idsFilters.Keys.Except(localStates.Select(idAccessor)).ToArray();
        var dbStates = await FetchFromDb(surplusIds.Select(e => idsFilters[e]));

        return FilterOutLocallyDeletedRows(localStates.Concat(dbStates.Except(localStates)));

        async Task<EventRow<TData>[]> FetchFromDb(IEnumerable<FilterRule> surplusIdsPredicates)
        {
            var result = new List<EventRow<TData>>();

            foreach (var chunk in surplusIdsPredicates.Chunk(500))
            {
                result.AddRange(await _dbContext.Events
                    .Where(_recurrentEventRowAskyFieldMap, chunk.Length == 1 ? chunk[0] : FilterRule.Or(chunk))
                    .ToArrayAsync());
            }

            return result.ToArray();
        }
    }

    private IEnumerable<EventRow<TData>> FilterOutLocallyDeletedRows(IEnumerable<EventRow<TData>> rows)
    {
        return rows.Where(row => !_localCache.IsRemoved(row));
    }

    private void AssertEventMightExist(RecurrentEvent<TData> @event, DateTimeOffset eventStart)
    {
        if (@event.MatchPeriod(eventStart) == null)
            throw new ArgumentException($"Recurrent event {@event.Id} hasn't event on {eventStart:O}",
                nameof(eventStart));
    }
}