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
        var row = EventRow<TData>.NewEvent(@event.Period, @event.Data);
        await _dbContext.Events.AddAsync(row);
        _cache.PushAdd(row);
        return @event;
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

    async Task<RecurrentEvent<TData>[]> IRecurrentEventCalendarInstance<TData>.GetManyAsync(
        FilterRule filter,
        SortRule? sortRule,
        PagingRule? pagingRule)
    {
        filter = filter.Replace(new RecurrentEventFilterRuleVisitor());

        var queryable = _dbContext.Events.Where(x => x.Type == EventType.RecurrentEvent)
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

        var states = await _dbContext.Events.Where(x =>
                x.Type == EventType.RecurrentEventState && x.RecurrentEventId == id &&
                (x.Effective.Start >= since.TotalMinutesSince1990() || x.MoveTo!.Start >= since))
            .ToArrayAsync();

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
        var rows = await _dbContext.Events.Where(x =>
                (x.Type == EventType.RecurrentEvent && x.Id == recurrentEventId) ||
                (x.Type == EventType.RecurrentEventState && x.RecurrentEventId == recurrentEventId))
            .ToArrayAsync();

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
        filter = filter.Replace(new DateTimeOffsetToMinutesSince1990FilterRuleVisitor(new[]
        {
            "period.start",
            "period.end",
        }));

        var queryable = _dbContext.Events.AsQueryable()
            .Where(_recurrentEventStateAskyFieldMap, filter)
            .Where(x => x.Type == EventType.RecurrentEventState)
            .Where(e => !e.Cancelled);

        var rows = await queryable.ToArrayAsync();

        return rows.Select(x => x.ToRecurrentEventState()).ToArray();
    }

    async Task<RecurrentEventState<TData>[]> IRecurrentEventCalendarInstance<TData>.GetManyStatesAsync(
        IEnumerable<RecurrentEventStateId> ids)
    {
        ids = ids.Distinct().ToArray();

        var local = _dbContext.Events.Local.Where(x => x.Type == EventType.RecurrentEventState &&
                                                       ids.Any(id =>
                                                           id.RecurrentEventId == x.RecurrentEventId &&
                                                           x.Effective.Start == id.EventStart.TotalMinutesSince1990()))
            .ToArray();

        if (local.Length == ids.Count())
            return local.Select(x => x.ToRecurrentEventState()).ToArray();

        var surplus = ids.Except(local.Select(x =>
                new RecurrentEventStateId(x.RecurrentEventId!.Value, Constants.J1_1990.AddMinutes(x.Effective.Start))))
            .ToArray();

        var surplusRows = await GetDbManyStatesAsync(surplus);

        return local.Concat(surplusRows).Select(x => x.ToRecurrentEventState()).ToArray();
    }

    private async Task<EventRow<TData>[]> GetDbManyStatesAsync(RecurrentEventStateId[] ids)
    {
        if (!ids.Any())
            return Array.Empty<EventRow<TData>>();

        var queryable = _dbContext.Events.Where(x => x.Type == EventType.RecurrentEventState);

        if (ids.Length == 1)
        {
            var first = ids.First();
            return await queryable
                .Where(x => first.RecurrentEventId == x.RecurrentEventId &&
                            first.EventStart.TotalMinutesSince1990() == x.Effective.Start)
                .ToArrayAsync();
        }

        var result = new List<EventRow<TData>>(ids.Length);

        foreach (var chunk in ids.Chunk(500))
        {
            var filters = FilterRule.Or(
                chunk.Select(e => FilterRule.And(
                    FilterRule.Eq("effective.start", e.EventStart.TotalMinutesSince1990()),
                    FilterRule.Eq("recurrentEventId", e.RecurrentEventId))));
            
            result.AddRange(await queryable.Where(_recurrentEventRowAskyFieldMap, filters).ToArrayAsync());
        }

        return result.ToArray();
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
        var filters = new EventFilters<TData>(from, to, dataFilter, _settings.TimeZone,
            filterOptimization ?? _settings.DbQueryOptimization);

        var result = await filters.Filter(_dbContext.Events.AsQueryable());

        return result.ToArray();
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
        var local = _dbContext.Events.Local.FirstOrDefault(x =>
            x.RecurrentEventId == recurrentEventId && x.Type == EventType.RecurrentEventState &&
            x.Effective.Start == dateTime.TotalMinutesSince1990());

        return local ?? await _dbContext.Events.FirstOrDefaultAsync(x =>
            x.RecurrentEventId == recurrentEventId && x.Type == EventType.RecurrentEventState &&
            x.Effective.Start == dateTime.TotalMinutesSince1990());
    }

    private async Task<EventRow<TData>> FindRequiredRecurrentEventStateAsync(Guid id, DateTimeOffset dateTime)
    {
        return await FindRecurrentEventStateAsync(id, dateTime) ?? throw CodedException.NotFound(new { id, dateTime });
    }

    private async Task<EventRow<TData>[]> GetManyAsync(IEnumerable<Guid> ids)
    {
        ids = ids.Distinct().ToArray();
        var local = _dbContext.Events.Local.Where(x => ids.Contains(x.Id)).ToArray();

        if (local.Length == ids.Count())
            return local;

        var surplus = ids.Except(local.Select(x => x.Id)).ToArray();
        var surplusRows = await _dbContext.Events.Where(x => surplus.Contains(x.Id)).ToArrayAsync();

        return local.Concat(surplusRows).ToArray();
    }

    private void AssertEventMightExist(RecurrentEvent<TData> @event, DateTimeOffset eventStart)
    {
        if (@event.MatchPeriod(eventStart) == null)
            throw new ArgumentException($"Recurrent event {@event.Id} hasn't event on {eventStart:O}",
                nameof(eventStart));
    }
}