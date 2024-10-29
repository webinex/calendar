using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Webinex.Calendar.Common;
using Webinex.Calendar.DataAccess;
using Webinex.Calendar.Events;

namespace Webinex.Calendar.Filters;

internal class DbQuery<TData> where TData : class, ICloneable
{
    private readonly DateTimeOffset _from;
    private readonly DateTimeOffset _to;
    private readonly Expression<Func<TData, bool>>? _dataFilter;
    private readonly string _timeZone;
    private readonly DbFilterOptimization _filteringOptionsFlags;

    public DbQuery(
        DateTimeOffset from,
        DateTimeOffset to,
        Expression<Func<TData, bool>>? dataFilter,
        string timeZone,
        DbFilterOptimization filteringOptionsFlags)
    {
        _dataFilter = dataFilter;
        _filteringOptionsFlags = filteringOptionsFlags;
        _timeZone = timeZone;
        _from = from.ToUtc();
        _to = to.ToUtc();
    }

    public EventRow<TData>[] ToArray(IEnumerable<EventRow<TData>> enumerable)
    {
        var provider = new EventFiltersProvider<TData>(
            From: _from,
            To: _to,
            DataFilter: _dataFilter,
            TimeZone: _timeZone,
            OneTime: true,
            DayOfMonth: true,
            DayOfWeek: true,
            Interval: true,
            State: true,
            Data: true,
            Precise: true);

        return enumerable.Where(provider.Create().Compile()).ToArray();
    }

    public async Task<EventRow<TData>[]> ToArrayAsync(IQueryable<EventRow<TData>> queryable)
    {
        var provider = new EventFiltersProvider<TData>(
            From: _from,
            To: _to,
            DataFilter: _dataFilter,
            TimeZone: _timeZone,
            OneTime: _filteringOptionsFlags.HasFlag(DbFilterOptimization.OneTime),
            DayOfMonth: _filteringOptionsFlags.HasFlag(DbFilterOptimization.DayOfMonth),
            DayOfWeek: _filteringOptionsFlags.HasFlag(DbFilterOptimization.DayOfWeek),
            Interval: _filteringOptionsFlags.HasFlag(DbFilterOptimization.Interval),
            State: _filteringOptionsFlags.HasFlag(DbFilterOptimization.State),
            Data: _filteringOptionsFlags.HasFlag(DbFilterOptimization.Data),
            Precise: _filteringOptionsFlags.HasFlag(DbFilterOptimization.Precise));

        var dbResult = await queryable.Where(provider.Create()).ToArrayAsync();
        await PopulateStatesWithRecurrentEvent(queryable, dbResult);

        // after not precise db filtering we should do precise filtering on the client
        provider.Precise = true;
        provider.Data = true;

        return dbResult.Where(provider.Create().Compile()).ToArray();
    }

    /// <summary>
    /// In rare cases EventRows of type RecurrentEventState might not have assigned RecurrentEvent, but have RecurrentEventId.
    /// In these cases we manually find RecurrentEvents and assign them
    /// </summary>
    private async Task PopulateStatesWithRecurrentEvent(
        IQueryable<EventRow<TData>> queryable,
        EventRow<TData>[] rows)
    {
        var statesWithoutRecurrentEvent = rows
            .Where(e => e.Type == EventType.RecurrentEventState)
            .Where(e => e.RecurrentEvent == null)
            .Where(e => e.RecurrentEventId.HasValue)
            .ToArray();

        var requiredRecurrentEvents = statesWithoutRecurrentEvent
            .Select(e => e.RecurrentEventId)
            .OfType<Guid>()
            .Distinct()
            .ToArray();

        if (requiredRecurrentEvents.Length == 0)
            return;

        var localRecurrentEvents = rows
            .Where(e => e.Type == EventType.RecurrentEvent)
            .Where(e => requiredRecurrentEvents.Contains(e.Id))
            .ToDictionary(e => e.Id);

        var surplusRecurrentEvents = requiredRecurrentEvents.Except(localRecurrentEvents.Select(e => e.Key)).ToArray();
        var dbRecurrentEvents = surplusRecurrentEvents.Length == 0
            ? new Dictionary<Guid, EventRow<TData>>(0)
            : await queryable.Where(e => surplusRecurrentEvents.Contains(e.Id)).ToDictionaryAsync(e => e.Id);

        foreach (var stateRow in statesWithoutRecurrentEvent)
        {
            var recurrentEventId = stateRow.RecurrentEventId!.Value;
            if (!localRecurrentEvents.TryGetValue(recurrentEventId, out var recurrentEvent) &&
                !dbRecurrentEvents.TryGetValue(recurrentEventId, out recurrentEvent))
            {
                throw new InvalidOperationException(
                    $"Unable to find RecurrentEvent '{recurrentEventId}' for RecurrentEventState '{stateRow.Id}'");
            }

            stateRow.RecurrentEvent = recurrentEvent;
        }
    }
}