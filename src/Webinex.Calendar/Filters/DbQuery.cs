using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Webinex.Calendar.Common;
using Webinex.Calendar.DataAccess;

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

        // after not precise db filtering we should do precise filtering on the client
        provider.Precise = true;
        provider.Data = true;

        return dbResult.Where(provider.Create().Compile()).ToArray();
    }
}