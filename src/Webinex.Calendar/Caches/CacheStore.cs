using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Webinex.Asky;
using Webinex.Calendar.Extensions;

namespace Webinex.Calendar.Caches;

internal interface ICacheStore<TData> where TData : class, ICloneable
{
    IReadOnlyDictionary<string, IEventEntityBase> RowById { get; }
    void Apply(IEnumerable<CacheEvent<TData>> events);
}

internal class CacheStore<TData> : IHostedService, ICacheStore<TData> where TData : class, ICloneable
{
    private readonly CacheTimer _timer;
    private Period<DateTimeOffset>? _period = null;

    private readonly IServiceProvider _serviceProvider;
    private readonly CalendarCacheOptions<TData> _options;
    private ConcurrentDictionary<string, IEventEntityBase> _rowById = new();
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    public CacheStore(
        IServiceProvider serviceProvider,
        CalendarCacheOptions<TData> options,
        ILogger<CacheStore<TData>> logger)
    {
        _serviceProvider = serviceProvider;
        _options = options;
        _timer = new CacheTimer(RefreshAsync, CalendarCacheOptions.TIMER_TICK, options.Tick!.Value, logger);
    }

    public IReadOnlyDictionary<string, IEventEntityBase> RowById => _rowById.AsReadOnly();

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await PreloadAsync();
        _timer.Start();
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _timer.Dispose();
        return Task.CompletedTask;
    }

    private async Task PreloadAsync()
    {
        using var scope = _serviceProvider.CreateScope();
        var eventRepository = scope.ServiceProvider.GetRequiredService<IEventRepository<TData>>();
        await PreloadAsync(eventRepository);
    }

    private async Task PreloadAsync(IEventRepository<TData> eventRepository)
    {
        var now = DateTimeOffset.UtcNow.StartOfMinute();
        _period = new Period<DateTimeOffset>(now.Subtract(_options.Previous!.Value), now.Add(_options.Next!.Value));

        var rows = await GetAllAsync(eventRepository, _period);
        _rowById = new ConcurrentDictionary<string, IEventEntityBase>(rows.ToDictionary(x => x.Id));
    }

    private async Task RefreshAsync()
    {
        using var scope = _serviceProvider.CreateScope();
        var eventRepository = scope.ServiceProvider.GetRequiredService<IEventRepository<TData>>();
        await _semaphore.WaitAsync();

        try
        {
            await RefreshAsync(eventRepository);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private async Task RefreshAsync(IEventRepository<TData> eventRepository)
    {
        if (_period == null)
            throw new InvalidOperationException($"{nameof(_period)} is null and it's unexpected at this moment");

        var now = DateTimeOffset.UtcNow.StartOfMinute();
        var period = new Period<DateTimeOffset>(now.Subtract(_options.Previous!.Value), now.Add(_options.Next!.Value));

        var rows = await GetAllAsync(eventRepository, period);
        _rowById = new ConcurrentDictionary<string, IEventEntityBase>(rows.ToDictionary(x => x.Id));
        _period = period;
    }

    private async Task<IReadOnlyCollection<IEventEntityBase>> GetAllAsync(
        IEventRepository<TData> eventRepository,
        Period<DateTimeOffset> period)
    {
        var filterRule = FilterRule.And(
            FilterRule.Lt("effective.start", period.End),
            FilterRule.Gt("effective.end", period.Start));

        return await eventRepository.GetAllAsync<IEventEntityBase>(filterRule);
    }

    public void Apply(IEnumerable<CacheEvent<TData>> events)
    {
        events = events?.ToArray() ?? throw new ArgumentNullException(nameof(events));
        _semaphore.Wait();

        try
        {
            foreach (var cacheEvent in events)
            {
                if (cacheEvent.Value is Event<TData> @event && !@event.Intersects(_period!))
                    continue;
                
                if (cacheEvent.Value is OccurrenceAdjustment<TData> state && !state.Period.Intersects(_period!) && state.MoveTo?.Intersects(_period!) != true)
                    continue;

                if (!cacheEvent.TryApply(_rowById))
                    // might store in another collection for a "not-loaded" or "not-received" for multiple deployments
                    throw new InvalidOperationException();
            }
        }
        finally
        {
            _semaphore.Release();
        }
    }
}