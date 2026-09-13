namespace Bangplanix.Core.Streaming;

public class MemoryCeilingGovernor
{
    private readonly long _maxAllowedBytes;
    private long _peakAllocatedBytes;
    private long _spillEventsTriggered;
    private long _throttlingDelaysTriggered;

    public long MaxAllowedBytes => _maxAllowedBytes;
    public long PeakAllocatedBytes => _peakAllocatedBytes;
    public long SpillEventsTriggered => _spillEventsTriggered;
    public long ThrottlingDelaysTriggered => _throttlingDelaysTriggered;

    /// <summary>
    /// Constructs a memory ceiling governor. Default limit: 128 MB (128 * 1024 * 1024 bytes).
    /// </summary>
    public MemoryCeilingGovernor(long maxAllowedBytes = 128 * 1024 * 1024)
    {
        _maxAllowedBytes = Math.Max(16 * 1024 * 1024, maxAllowedBytes); // minimum 16MB
    }

    public long GetCurrentAllocatedMemory()
    {
        var mem = GC.GetTotalMemory(false);
        if (mem > _peakAllocatedBytes)
        {
            _peakAllocatedBytes = mem;
        }
        return mem;
    }

    public bool IsMemoryCeilingExceeded()
    {
        var current = GetCurrentAllocatedMemory();
        return current >= _maxAllowedBytes;
    }

    public async ValueTask EnforceCeilingAsync(CancellationToken cancellationToken = default)
    {
        var current = GetCurrentAllocatedMemory();

        // If allocated memory exceeds 80% of ceiling, request GC collect ephemeral generation
        if (current >= (_maxAllowedBytes * 0.8))
        {
            _spillEventsTriggered++;
            GC.Collect(0, GCCollectionMode.Optimized, blocking: false);

            // Re-check after ephemeral collection
            current = GetCurrentAllocatedMemory();
            if (current >= _maxAllowedBytes)
            {
                _throttlingDelaysTriggered++;
                // Backpressure throttle 5ms to allow GC and I/O pipelines to catch up
                await Task.Delay(5, cancellationToken);
                GC.Collect(1, GCCollectionMode.Optimized, blocking: true);
            }
        }
    }
}
