using BenchmarkDotNet.Attributes;
using Bangplanix.Core.Streaming;
using Bangplanix.Engine.Streaming;

namespace Bangplanix.Benchmarks;

[MemoryDiagnoser]
public class MassiveDataStreamingBenchmarks
{
    private List<Dictionary<string, object?>> _testRows = null!;
    private MassiveDataStreamingEngine _streamingEngine = null!;

    [GlobalSetup]
    public void Setup()
    {
        _streamingEngine = new MassiveDataStreamingEngine();
        _testRows = new List<Dictionary<string, object?>>(50_000);
        for (int i = 0; i < 50_000; i++)
        {
            _testRows.Add(new Dictionary<string, object?>
            {
                ["Id"] = i + 1,
                ["Customer"] = $"Enterprise Customer {i % 500}",
                ["Amount"] = 1500.50m + (i % 100),
                ["Status"] = i % 2 == 0 ? "Completed" : "Pending",
                ["Timestamp"] = DateTime.UtcNow
            });
        }
    }

    private async IAsyncEnumerable<IReadOnlyDictionary<string, object?>> GetTestRowsAsync()
    {
        foreach (var row in _testRows)
        {
            yield return row;
        }
        await Task.CompletedTask;
    }

    [Benchmark(Baseline = true)]
    public async Task DiskSpillRowBuffer_AppendAndRead50kRows()
    {
        await using var buffer = new DiskSpillRowBuffer(maxMemoryRows: 5000);
        foreach (var row in _testRows)
        {
            await buffer.AddRowAsync(row);
        }

        int readCount = 0;
        await foreach (var _ in buffer.ReadAllAsync())
        {
            readCount++;
        }
    }

    [Benchmark]
    public async Task MassiveDataStreaming_StreamToCsv()
    {
        using var ms = new MemoryStream();
        await _streamingEngine.StreamToCsvAsync(GetTestRowsAsync(), ms);
    }

    [Benchmark]
    public async Task MassiveDataStreaming_StreamToNdjson()
    {
        using var ms = new MemoryStream();
        await _streamingEngine.StreamToNdjsonAsync(GetTestRowsAsync(), ms);
    }
}
