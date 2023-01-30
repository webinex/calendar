using Microsoft.EntityFrameworkCore;
using Webinex.Asky;
using Webinex.Calendar.Common;
using Webinex.Calendar.DataAccess;
using Webinex.Calendar.Events;
using Webinex.Calendar.Filters;
using Webinex.Coded;

namespace Webinex.Calendar;

internal class Calendar<TData> : ICalendar<TData>
    where TData : class, ICloneable
{
    private readonly ICalendarDbContext<TData> _dbContext;
    private readonly IAskyFieldMap<TData> _dataFieldMap;

    public Calendar(ICalendarDbContext<TData> dbContext, IAskyFieldMap<TData> dataFieldMap)
    {
        _dbContext = dbContext;
        _dataFieldMap = dataFieldMap;
    }

    public async Task<OneTimeEvent<TData>?> GetOneTimeEventAsync(Guid id)
    {
        var row = await FindAsync(id);
        return row?.ToOneTimeEvent();
    }

    public async Task<OneTimeEvent<TData>> AddOneTimeEventAsync(OneTimeEvent<TData> @event)
    {
        @event = @event ?? throw new ArgumentNullException(nameof(@event));
        var row = EventRow<TData>.NewEvent(@event.Start, @event.End, @event.Data);
        await _dbContext.Events.AddAsync(row);
        return @event;
    }

    public async Task<OneTimeEvent<TData>> UpdateDataAsync(OneTimeEvent<TData> @event, TData data)
    {
        var row = await FindRequiredAsync(@event.Id);
        row.SetOneTimeEventData(data);
        return row.ToOneTimeEvent();
    }

    public async Task DeleteAsync(OneTimeEvent<TData> @event)
    {
        var row = await FindRequiredAsync(@event.Id);
        _dbContext.Events.Remove(row);
    }

    public async Task<RecurrentEvent<TData>?> GetRecurrentAsync(Guid id)
    {
        var row = await FindAsync(id, EventRowType.RecurrentEvent);
        return row?.ToRecurrentEvent();
    }

    public async Task<Event<TData>> AddOrUpdateRecurrentDataAsync(
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

        return new Event<TData>(null, @event.Id, stateRow.Effective.Start, stateRow.Effective.End!.Value, data, false);
    }

    public async Task<Event<TData>> AddRecurrentStateAsync(
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

        return new Event<TData>(null, @event.Id, dataRow.Effective.Start, dataRow.Effective.End!.Value, data, false);
    }

    public async Task<Event<TData>> UpdateRecurrentDataAsync(
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

        return new Event<TData>(null, @event.Id, dataRow.Effective.Start, dataRow.Effective.End!.Value, data, false);
    }

    public async Task<RecurrentEvent<TData>> AddRecurrentEventAsync(RecurrentEvent<TData> @event)
    {
        @event = @event ?? throw new ArgumentNullException(nameof(@event));
        var row = EventRow<TData>.NewRecurrentEvent(@event.Id, @event.Repeat, @event.Effective, @event.Data);
        await _dbContext.Events.AddAsync(row);
        return @event;
    }

    public async Task MoveRecurrentEventAsync(RecurrentEvent<TData> @event, DateTimeOffset eventStart, Period moveTo)
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

    public async Task CancelRecurrentEventSinceAsync(Guid id, DateTimeOffset since)
    {
        var row = await FindRequiredAsync(id, EventRowType.RecurrentEvent);
        row.Effective = new OpenPeriod(row.Effective.Start, since);
    }

    public async Task CancelOneRecurrentEventAsync(RecurrentEvent<TData> @event, DateTimeOffset eventStart)
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

    public async Task<Event<TData>[]> GetAllAsync(
        DateTimeOffset from,
        DateTimeOffset to,
        FilterRule? dataFilterRule = null)
    {
        var dataFilter = dataFilterRule != null ? AskyExpressionFactory.Create(_dataFieldMap, dataFilterRule) : null;

        var queryable = _dbContext.Events.AsQueryable()
            .Where(EventFilterFactory.Create(from, to, dataFilter));

        var rows = await queryable.ToArrayAsync();

        var oneTimeEvents = rows.Where(x => x.Type == EventRowType.OneTimeEvent).ToArray();

        var recurrentEventStates = rows
            .Where(x => x.Type == EventRowType.RecurrentEventState)
            .Select(x => x.ToRecurrentEventState()).ToArray();

        var recurrentEventStateById = recurrentEventStates.ToLookup(x => x.RecurrentEventId);

        var recurrentEvents = rows.Where(x => x.Type == EventRowType.RecurrentEvent).ToArray();
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

    private async Task<EventRow<TData>?> FindAsync(Guid id, EventRowType? type = null)
    {
        var eventRow = await _dbContext.Events.FindAsync(id);
        if (type.HasValue && eventRow != null && eventRow.Type != type)
            throw new InvalidOperationException(
                $"{nameof(EventRow<ICloneable>)} {id} type {eventRow.Type:G} doesn't match required type {type:G}");

        return eventRow;
    }

    private async Task<EventRow<TData>> FindRequiredAsync(Guid id, EventRowType? type = null)
    {
        return await FindAsync(id, type) ?? throw CodedException.NotFound(id);
    }

    private async Task<EventRow<TData>?> FindRecurrentEventStateAsync(Guid id, DateTimeOffset dateTime)
    {
        var local = _dbContext.Events.Local.FirstOrDefault(x =>
            x.RecurrentEventId == id && x.Type == EventRowType.RecurrentEventState && x.Effective.Start == dateTime);

        return local ?? await _dbContext.Events.FirstOrDefaultAsync(x =>
            x.RecurrentEventId == id && x.Type == EventRowType.RecurrentEventState && x.Effective.Start == dateTime);
    }

    private async Task<EventRow<TData>> FindRequiredRecurrentEventDataAsync(Guid id, DateTimeOffset dateTime)
    {
        return await FindRecurrentEventStateAsync(id, dateTime) ?? throw CodedException.NotFound(new { id, dateTime });
    }
}