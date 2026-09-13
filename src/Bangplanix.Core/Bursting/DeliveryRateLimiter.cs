namespace Bangplanix.Core.Bursting;

/// <summary>
/// Thread-safe Egress Rate Limiter and Throttling Governor preventing delivery flood / anti-spam blockades.
/// </summary>
public sealed class DeliveryRateLimiter
{
    private readonly SemaphoreSlim _lock = new(1, 1);
    private int _tokens;
    private DateTime _lastRefillUtc = DateTime.UtcNow;

    public int MaxDispatchesPerSecond { get; set; } = 50;
    public bool IsEnabled { get; set; } = true;

    public DeliveryRateLimiter(int maxDispatchesPerSecond = 50)
    {
        MaxDispatchesPerSecond = Math.Max(1, maxDispatchesPerSecond);
        _tokens = MaxDispatchesPerSecond;
    }

    /// <summary>
    /// Acquires permission to dispatch one delivery message, pausing if the token bucket is empty.
    /// </summary>
    public async Task AcquireAsync(CancellationToken cancellationToken = default)
    {
        if (!IsEnabled) return;

        while (!cancellationToken.IsCancellationRequested)
        {
            await _lock.WaitAsync(cancellationToken);
            try
            {
                RefillTokens();

                if (_tokens > 0)
                {
                    _tokens--;
                    return;
                }
            }
            finally
            {
                _lock.Release();
            }

            // Wait 20ms before retrying token acquisition
            await Task.Delay(20, cancellationToken);
        }
    }

    private void RefillTokens()
    {
        var now = DateTime.UtcNow;
        double elapsedSeconds = (now - _lastRefillUtc).TotalSeconds;

        if (elapsedSeconds >= 1.0)
        {
            _tokens = MaxDispatchesPerSecond;
            _lastRefillUtc = now;
        }
        else if (elapsedSeconds > 0)
        {
            int tokensToAdd = (int)(elapsedSeconds * MaxDispatchesPerSecond);
            if (tokensToAdd > 0)
            {
                _tokens = Math.Min(MaxDispatchesPerSecond, _tokens + tokensToAdd);
                _lastRefillUtc = now;
            }
        }
    }
}
