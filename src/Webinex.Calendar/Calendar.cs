using Microsoft.EntityFrameworkCore;
using Webinex.Asky;
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

    public Calendar(ICalendarDbContext<TData> dbContext, IAskyFieldMap<TData> dataFieldMap)
    {
        _dbContext = dbContext;
        _dataFieldMap = dataFieldMap;
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
        return @event;
    }

    async Task<OneTimeEvent<TData>> IOneTimeEventCalendarInstance<TData>.UpdateDataAsync(
        OneTimeEvent<TData> @event,
        TData data)
    {
        var row = await FindRequiredAsync(@event.Id);
        row.SetOneTimeEventData(data);
        return row.ToOneTimeEvent();
    }

    async Task IOneTimeEventCalendarInstance<TData>.DeleteAsync(OneTimeEvent<TData> @event)
    {
        var row = await FindRequiredAsync(@event.Id);
        _dbContext.Events.Remove(row);
    }

    async Task IOneTimeEventCalendarInstance<TData>.CancelAsync(OneTimeEvent<TData> @event)
    {
        var row = await FindRequiredAsync(@event.Id, EventType.RecurrentEvent);
        row.Cancel();
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

    async Task<Event<TData>> IRecurrentEventCalendarInstance<TData>.SaveDataAsync(
        RecurrentEvent<TData> @event,
        DateTimeOffset eventStart,
        TData data)
    {
        @event = @event ?? throw new ArgumentNullException(nameof(@event));
        data = data ?? throw new ArgumentNullException(nameof(data));

        // if (!@event.HasEventOnDate(date))
        //     throw new ArgumentException("Recurrent event hasn't event on date", nameof(date));

        var stateRow = await FindRecurrentEventStateAsync(@event.Id, eventStart);

        if (stateRow == null)
        {
            var eventEnd = eventStart.AddMinutes(@event.DurationMinutes());
            stateRow = EventRow<TData>.NewRecurrentEventState(@event.Id, eventStart, eventEnd, data, null, false);
            await _dbContext.Events.AddAsync(stateRow);
        }
        else
        {
            stateRow.SetRecurrentEventData(data);
        }

        return new Event<TData>(null, @event.Id, stateRow.Effective.ToPeriod(), data, false,
            stateRow.MoveTo != null ? stateRow.Effective.ToPeriod() : null);
    }

    async Task<Event<TData>> IRecurrentEventCalendarInstance<TData>.AddDataAsync(
        RecurrentEvent<TData> @event,
        DateTimeOffset start,
        TData data)
    {
        @event = @event ?? throw new ArgumentNullException(nameof(@event));
        data = data ?? throw new ArgumentNullException(nameof(data));

        // if (!@event.HasEventOnDate(date))
        //     throw new ArgumentException("Recurrent event hasn't event on date", nameof(date));

        var dataRow =
            EventRow<TData>.NewRecurrentEventState(@event.Id, start, start.AddMinutes(@event.DurationMinutes()), data,
                null, false);
        await _dbContext.Events.AddAsync(dataRow);

        return new Event<TData>(null, @event.Id, dataRow.Effective.ToPeriod(), data, false,
            dataRow.MoveTo != null ? dataRow.Effective.ToPeriod() : null);
    }

    async Task<Event<TData>> IRecurrentEventCalendarInstance<TData>.UpdateDataAsync(
        RecurrentEvent<TData> @event,
        DateTimeOffset date,
        TData data)
    {
        @event = @event ?? throw new ArgumentNullException(nameof(@event));
        data = data ?? throw new ArgumentNullException(nameof(data));

        // if (!@event.HasEventOnDate(date))
        //     throw new ArgumentException("Recurrent event hasn't event on date", nameof(date));

        var dataRow = await FindRequiredRecurrentEventDataAsync(@event.Id, date);
        dataRow.SetRecurrentEventData(data);

        return new Event<TData>(null, @event.Id, dataRow.Effective.ToPeriod(), data, false,
            dataRow.MoveTo != null ? dataRow.Effective.ToPeriod() : null);
    }

    async Task IRecurrentEventCalendarInstance<TData>.DeleteStateAsync(RecurrentEventStateId id)
    {
        var state = await FindRecurrentEventStateAsync(id.RecurrentEventId, id.EventStart);
        if (state == null)
            throw CodedException.NotFound(id);

        _dbContext.Events.Remove(state);
    }

    async Task<RecurrentEvent<TData>> IRecurrentEventCalendarInstance<TData>.AddAsync(RecurrentEvent<TData> @event)
    {
        @event = @event ?? throw new ArgumentNullException(nameof(@event));
        var row = EventRow<TData>.NewRecurrentEvent(@event.Id, @event.Repeat, @event.Effective, @event.Data);
        await _dbContext.Events.AddAsync(row);
        return @event;
    }

    async Task IRecurrentEventCalendarInstance<TData>.MoveAsync(
        RecurrentEvent<TData> @event,
        DateTimeOffset eventStart,
        Period moveTo)
    {
        var stateRow = await FindRecurrentEventStateAsync(@event.Id, eventStart);

        if (stateRow == null)
        {
            var eventEnd = eventStart.AddMinutes(@event.DurationMinutes());
            stateRow = EventRow<TData>.NewRecurrentEventState(@event.Id, eventStart, eventEnd,
                (TData)@event.Data.Clone(), moveTo, false);
            await _dbContext.Events.AddAsync(stateRow);
        }
        else
        {
            stateRow.Move(moveTo);
        }
    }

    async Task IRecurrentEventCalendarInstance<TData>.CancelAsync(Guid id, DateTimeOffset since)
    {
        var row = await FindRequiredAsync(id, EventType.RecurrentEvent);

        if (row.Effective.End < since.TotalMinutesSince1990())
            throw CodedException.Invalid("Should be less than current end");

        row.Effective = new OpenPeriodMinutesSince1990(row.Effective.Start, since.TotalMinutesSince1990());

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

        _dbContext.Events.RemoveRange(remove);
    }

    async Task IRecurrentEventCalendarInstance<TData>.CancelAppearanceAsync(
        RecurrentEvent<TData> @event,
        DateTimeOffset eventStart)
    {
        var stateRow = await FindRecurrentEventStateAsync(@event.Id, eventStart);

        if (stateRow == null)
        {
            var eventEnd = eventStart.AddMinutes(@event.DurationMinutes());
            stateRow = EventRow<TData>.NewRecurrentEventState(@event.Id, eventStart, eventEnd,
                (TData)@event.Data.Clone(), null, true);
            await _dbContext.Events.AddAsync(stateRow);
        }
        else
        {
            stateRow.Cancel();
        }
    }

    async Task IRecurrentEventCalendarInstance<TData>.DeleteAsync(Guid recurrentEventId)
    {
        var rows = await _dbContext.Events.Where(x =>
                x.Type == EventType.RecurrentEvent || (x.Type == EventType.RecurrentEventState &&
                                                       (x.Id == recurrentEventId ||
                                                        x.RecurrentEventId == recurrentEventId)))
            .ToArrayAsync();

        var recurrentEvent = rows.FirstOrDefault(x => x.Id == recurrentEventId)?.ToRecurrentEvent();
        if (recurrentEvent == null)
            throw CodedException.NotFound(recurrentEventId);

        _dbContext.Events.RemoveRange(rows);
    }

    async Task<RecurrentEventState<TData>?> IRecurrentEventCalendarInstance<TData>.GetStateAsync(
        RecurrentEventStateId id)
    {
        var result = await FindRecurrentEventStateAsync(id.RecurrentEventId, id.EventStart);
        return result?.ToRecurrentEventState();
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

        var surplusRows = await _dbContext.Events.Where(x =>
                x.Type == EventType.RecurrentEventState && surplus.Any(id =>
                    id.RecurrentEventId == x.RecurrentEventId &&
                    id.EventStart.TotalMinutesSince1990() == x.Effective.Start))
            .ToArrayAsync();

        return local.Concat(surplusRows).Select(x => x.ToRecurrentEventState()).ToArray();
    }

    public async Task<Event<TData>[]> GetCalculatedAsync(
        DateTimeOffset from,
        DateTimeOffset to,
        FilterRule? dataFilterRule = null)
    {
        var dataFilter = dataFilterRule != null ? AskyExpressionFactory.Create(_dataFieldMap, dataFilterRule) : null;

        var queryable = _dbContext.Events.AsQueryable()
            .Where(EventFilterFactory.Create(from, to, dataFilter));

        var rows = await queryable.ToArrayAsync();

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

        return result;
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

    private async Task<EventRow<TData>> FindRequiredRecurrentEventDataAsync(Guid id, DateTimeOffset dateTime)
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
}