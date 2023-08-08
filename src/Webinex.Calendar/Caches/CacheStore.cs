using System.Collections.Concurrent;
using System.Collections.Immutable;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Webinex.Calendar.Common;
using Webinex.Calendar.DataAccess;

namespace Webinex.Calendar.Caches;

internal interface ICacheStore<TData> where TData : class, ICloneable
{
    ImmutableDictionary<EventRowId, EventRow<TData>> RowById { get; }
    void Apply(IEnumerable<CacheEvent<TData>> events);
}

internal class CacheStore<TData> : IHostedService, ICacheStore<TData> where TData : class, ICloneable
{
    private readonly CacheTimer _timer;
    private Period? _period = null;

    private readonly IServiceProvider _serviceProvider;
    private readonly CalendarCacheOptions<TData> _options;
    private ConcurrentDictionary<EventRowId, EventRow<TData>> _rowById = new();
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

    public ImmutableDictionary<EventRowId, EventRow<TData>> RowById => _rowById.ToImmutableDictionary();

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
        var dbContext = scope.ServiceProvider.GetRequiredService<ICalendarDbContext<TData>>();
        await PreloadAsync(dbContext);
    }

    private async Task PreloadAsync(ICalendarDbContext<TData> dbContext)
    {
        var now = DateTimeOffset.UtcNow.StartOfMinute();
        _period = new Period(now.Subtract(_options.Previous!.Value), now.Add(_options.Next!.Value));

        var rows = await GetAllAsync(dbContext, _period);
        _rowById = new ConcurrentDictionary<EventRowId, EventRow<TData>>(rows.ToDictionary(x => x.GetEventRowId()));
    }

    private async Task RefreshAsync()
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ICalendarDbContext<TData>>();
        await _semaphore.WaitAsync();

        try
        {
            await RefreshAsync(dbContext);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private async Task RefreshAsync(ICalendarDbContext<TData> dbContext)
    {
        if (_period == null)
            throw new InvalidOperationException($"{nameof(_period)} is null and it's unexpected at this moment");

        var now = DateTimeOffset.UtcNow.StartOfMinute();
        var period = new Period(now.Subtract(_options.Previous!.Value), now.Add(_options.Next!.Value));

        var rows = await GetAllAsync(dbContext, period);
        _rowById = new ConcurrentDictionary<EventRowId, EventRow<TData>>(rows.ToDictionary(x => x.GetEventRowId()));
        _period = period;
    }

    private async Task<EventRow<TData>[]> GetAllAsync(ICalendarDbContext<TData> dbContext, Period period)
    {
        return await dbContext.Events.Where(EventRow<TData>.InPeriodExpression(period)).ToArrayAsync();
    }

    public void Apply(IEnumerable<CacheEvent<TData>> events)
    {
        events = events?.ToArray() ?? throw new ArgumentNullException(nameof(events));
        _semaphore.Wait();

        try
        {
            foreach (var cacheEvent in events)
            {
                if (cacheEvent.Value.InPeriod(_period!))
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