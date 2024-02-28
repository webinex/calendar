using System.Collections.Concurrent;
using System.Collections.Immutable;
using Microsoft.EntityFrameworkCore;
using Webinex.Asky;
using Webinex.Calendar.DataAccess;
using Webinex.Calendar.Filters;

namespace Webinex.Calendar.Caches;

internal interface ICache<TData> where TData : class, ICloneable
{
    bool TryGetAll(
        DateTimeOffset from,
        DateTimeOffset to,
        FilterRule? dataFilterRule,
        DbFilterOptimization? filterOptimization,
        out ImmutableArray<EventRow<TData>>? result);

    void Push(IEnumerable<CacheEvent<TData>> values);
}

internal class Cache<TData> : ICache<TData>
    where TData : class, ICloneable
{
    private readonly ConcurrentQueue<CacheEvent<TData>> _queue = new();
    private readonly ICacheStore<TData> _store;
    private readonly CalendarCacheOptions<TData> _options;
    private readonly IAskyFieldMap<TData> _dataFieldMap;
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private readonly ICalendarOptions<TData> _calendarOptions;

    public Cache(
        ICacheStore<TData> store,
        CalendarCacheOptions<TData> options,
        IAskyFieldMap<TData> dataFieldMap,
        ICalendarDbContext<TData> dbContext,
        ICalendarOptions<TData> calendarOptions)
    {
        _store = store;
        _options = options;
        _dataFieldMap = dataFieldMap;
        _calendarOptions = calendarOptions;

        ((DbContext)dbContext).SavedChanges += (_, _) => Flush();
    }

    public bool TryGetAll(
        DateTimeOffset from,
        DateTimeOffset to,
        FilterRule? dataFilterRule,
        DbFilterOptimization? filterOptimization,
        out ImmutableArray<EventRow<TData>>? result)
    {
        result = null;

        if (_options.Min() > from || _options.Max() < to)
            return false;

        var dictionary = new ConcurrentDictionary<EventRowId, EventRow<TData>>(_store.RowById);
        foreach (var cacheEvent in _queue)
            cacheEvent.TryApply(dictionary);

        var dataFilter = dataFilterRule != null ? AskyExpressionFactory.Create(_dataFieldMap, dataFilterRule) : null;
        var filter = new EventFiltersFactory<TData>(
            from,
            to,
            dataFilter,
            filterOptimization ?? _calendarOptions.DbFilterOptimization);
        result = filter.Filter(dictionary.Values).ToImmutableArray();
        return true;
    }

    public void Push(IEnumerable<CacheEvent<TData>> values)
    {
        values = values?.ToArray() ?? throw new ArgumentNullException(nameof(values));
        _semaphore.Wait();

        try
        {
            foreach (var value in values)
                _queue.Enqueue(value);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private void Flush()
    {
        _semaphore.Wait();

        try
        {
            _store.Apply(_queue.ToArray());
            _queue.Clear();
        }
        finally
        {
            _semaphore.Release();
        }
    }
}