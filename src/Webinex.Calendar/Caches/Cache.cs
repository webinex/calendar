using System.Collections.Concurrent;
using System.Linq.Expressions;
using Webinex.Asky;

namespace Webinex.Calendar.Caches;

internal interface ICache<TData> where TData : class, ICloneable
{
    bool TryGetAll(
        Period<DateTimeOffset> period,
        FilterRule? dataFilterRule,
        out IReadOnlyCollection<IEventEntityBase>? result);

    void Push(IEnumerable<CacheEvent<TData>> values);
    void Flush();
}

internal class Cache<TData> : ICache<TData>
    where TData : class, ICloneable
{
    private readonly ConcurrentQueue<CacheEvent<TData>> _queue = new();
    private readonly ICacheStore<TData> _store;
    private readonly CalendarCacheOptions<TData> _options;
    private readonly IAskyFieldMap<TData> _dataFieldMap;
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    public Cache(
        ICacheStore<TData> store,
        CalendarCacheOptions<TData> options,
        IAskyFieldMap<TData> dataFieldMap)
    {
        _store = store;
        _options = options;
        _dataFieldMap = dataFieldMap;
    }

    public bool TryGetAll(
        Period<DateTimeOffset> period,
        FilterRule? dataFilterRule,
        out IReadOnlyCollection<IEventEntityBase>? result)
    {
        result = null;

        if (_options.Min() > period.Start || _options.Max() < period.End)
            return false;

        var dictionary = new ConcurrentDictionary<string, IEventEntityBase>(_store.RowById);
        foreach (var cacheEvent in _queue)
            cacheEvent.TryApply(dictionary);

        var dataFilter = dataFilterRule != null ? AskyExpressionFactory.Create(_dataFieldMap, dataFilterRule) : null;
        result = Match(dictionary.Values, period, dataFilter);
        return true;
    }

    private IReadOnlyCollection<IEventEntityBase> Match(
        IEnumerable<IEventEntityBase> events,
        Period<DateTimeOffset> period,
        Expression<Func<TData, bool>>? dataPredicateExpression)
    {
        var dataPredicate = dataPredicateExpression?.Compile();
        Func<IEventEntityBase, bool>? eventDataPredicate = dataPredicate != null
            ? eventBase => (eventBase is Event<TData> @event && dataPredicate(@event.Data)) ||
                             (eventBase is OccurrenceAdjustment<TData> eventState && (eventState.Data == null ||
                                                                            dataPredicate(eventState.Data)))
            : null;
        return events.Where(x => x.Period.Intersects(period) && eventDataPredicate?.Invoke(x) != false).ToArray();
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

    public void Flush()
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