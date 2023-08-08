using Microsoft.Extensions.Logging;

namespace Webinex.Calendar.Caches;

internal class CacheTimer : IDisposable
{
    private readonly Func<Task> _callback;
    private readonly TimeSpan _period;
    private readonly ILogger _logger;
    private readonly CancellationTokenSource _cancellationTokenSource;
    private readonly PeriodicTimer _periodicTimer;
    private DateTimeOffset? _lastExecutedAt = null;

    public CacheTimer(Func<Task> callback, TimeSpan tick, TimeSpan period, ILogger logger)
    {
        _callback = callback;
        _period = period;
        _logger = logger;
        _cancellationTokenSource = new CancellationTokenSource();
        _periodicTimer = new PeriodicTimer(tick);

        _cancellationTokenSource.Token.Register(() => _periodicTimer.Dispose());
    }

    public void Start()
    {
        Task.Run(async () =>
        {
            while (!_cancellationTokenSource.IsCancellationRequested)
            {
                await _periodicTimer.WaitForNextTickAsync(_cancellationTokenSource.Token);

                if (_cancellationTokenSource.IsCancellationRequested)
                    return;

                if (_lastExecutedAt.HasValue && _lastExecutedAt.Value.Add(_period) > DateTimeOffset.UtcNow)
                    continue;

                if (await TryTickAsync())
                    _lastExecutedAt = DateTimeOffset.UtcNow;
            }
        });
    }

    private async Task<bool> TryTickAsync()
    {
        try
        {
            await _callback();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Tick failed");
            return false;
        }
    }

    public void Dispose()
    {
        _cancellationTokenSource.Dispose();
    }
}