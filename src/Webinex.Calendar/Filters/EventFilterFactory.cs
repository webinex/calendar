using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Webinex.Calendar.Common;
using Webinex.Calendar.DataAccess;

namespace Webinex.Calendar.Filters;

internal class EventFilters<TData> where TData : class, ICloneable
{
    private readonly DateTimeOffset _from;
    private readonly DateTimeOffset _to;
    private readonly Expression<Func<TData, bool>>? _dataFilter;
    private readonly string _timeZone;
    private readonly DbFilterOptimization _filteringOptionsFlags;

    public EventFilters(
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

    public IEnumerable<EventRow<TData>> Filter(IEnumerable<EventRow<TData>> enumerable)
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
            // We always enable these options, because we already have client collection
            Data: true,
            Precise: true);

        return enumerable.Where(provider.Create().Compile()).ToArray();
    }

    public async Task<IEnumerable<EventRow<TData>>> Filter(IQueryable<EventRow<TData>> queryable)
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

        // If both flags are set, precise filtering already happened, no sense to do it again
        if (_filteringOptionsFlags.HasFlag(DbFilterOptimization.Data) &&
            _filteringOptionsFlags.HasFlag(DbFilterOptimization.Precise))
            return dbResult;

        // after not precise db filtering we should do precise filtering on the client
        provider.Precise = true;
        provider.Data = true;

        return dbResult.Where(provider.Create().Compile()).ToArray();
    }
}