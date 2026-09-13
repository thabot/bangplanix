using System.Globalization;
using System.Text;
using System.Text.Json;
using Bangplanix.Core.Streaming;

namespace Bangplanix.Engine.Streaming;

public class RunningColumnAggregate
{
    public string ColumnName { get; set; } = string.Empty;
    public long Count { get; set; }
    public long NullCount { get; set; }
    public double Sum { get; set; }
    public double Min { get; set; } = double.MaxValue;
    public double Max { get; set; } = double.MinValue;
    public double Mean => Count > 0 ? Sum / Count : 0;
    
    // Welford's algorithm for single-pass sample variance and standard deviation
    private double _m2;

    public void Accumulate(object? value)
    {
        if (value == null || value is DBNull)
        {
            NullCount++;
            return;
        }

        double numericVal;
        if (value is double d) numericVal = d;
        else if (value is float f) numericVal = f;
        else if (value is int i) numericVal = i;
        else if (value is long l) numericVal = l;
        else if (value is decimal dec) numericVal = (double)dec;
        else if (double.TryParse(value.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed))
        {
            numericVal = parsed;
        }
        else
        {
            // Non-numeric column
            Count++;
            return;
        }

        Count++;
        Sum += numericVal;
        if (numericVal < Min) Min = numericVal;
        if (numericVal > Max) Max = numericVal;

        // Welford variance update
        double delta = numericVal - (Sum / Count);
        double delta2 = numericVal - (Sum / (Count == 1 ? 1 : Count));
        _m2 += delta * delta2;
    }

    public double Variance => Count > 1 ? _m2 / (Count - 1) : 0;
    public double StandardDeviation => Math.Sqrt(Variance);
}

public class StreamingReportResult
{
    public long TotalRowsProcessed { get; set; }
    public long SpilledRowsCount { get; set; }
    public long PeakAllocatedMemoryBytes { get; set; }
    public TimeSpan ElapsedDuration { get; set; }
    public Dictionary<string, RunningColumnAggregate> Aggregates { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

public class MassiveDataStreamingEngine
{
    private readonly MemoryCeilingGovernor _governor;
    private readonly int _maxMemoryRows;

    public MassiveDataStreamingEngine(long maxAllowedMemoryBytes = 128 * 1024 * 1024, int maxMemoryRows = 5000)
    {
        _governor = new MemoryCeilingGovernor(maxAllowedMemoryBytes);
        _maxMemoryRows = maxMemoryRows;
    }

    public async Task<StreamingReportResult> StreamToCsvAsync(
        IAsyncEnumerable<IReadOnlyDictionary<string, object?>> rowSource,
        Stream outputStream,
        IEnumerable<string>? selectColumns = null,
        CancellationToken cancellationToken = default)
    {
        var startTime = DateTime.UtcNow;
        var aggregates = new Dictionary<string, RunningColumnAggregate>(StringComparer.OrdinalIgnoreCase);
        long rowCount = 0;

        await using var buffer = new DiskSpillRowBuffer(_maxMemoryRows);
        await using var writer = new StreamWriter(outputStream, Encoding.UTF8, bufferSize: 64 * 1024, leaveOpen: true);

        List<string>? headers = selectColumns?.ToList();
        bool headerWritten = false;

        await foreach (var row in rowSource.WithCancellation(cancellationToken))
        {
            rowCount++;
            await buffer.AddRowAsync(row, cancellationToken);

            if (rowCount % 1000 == 0)
            {
                await _governor.EnforceCeilingAsync(cancellationToken);
            }
        }

        // Stream from buffer to CSV
        await foreach (var row in buffer.ReadAllAsync(cancellationToken))
        {
            if (!headerWritten)
            {
                headers ??= row.Keys.ToList();
                await writer.WriteLineAsync(string.Join(",", headers.Select(EscapeCsvField)));
                headerWritten = true;

                foreach (var col in headers)
                {
                    aggregates[col] = new RunningColumnAggregate { ColumnName = col };
                }
            }

            var values = new string[headers.Count];
            for (int i = 0; i < headers.Count; i++)
            {
                var col = headers[i];
                row.TryGetValue(col, out var val);
                aggregates[col].Accumulate(val);
                values[i] = EscapeCsvField(val?.ToString() ?? string.Empty);
            }

            await writer.WriteLineAsync(string.Join(",", values));
        }

        await writer.FlushAsync(cancellationToken);

        return new StreamingReportResult
        {
            TotalRowsProcessed = rowCount,
            SpilledRowsCount = buffer.SpilledRowCount,
            PeakAllocatedMemoryBytes = _governor.PeakAllocatedBytes,
            ElapsedDuration = DateTime.UtcNow - startTime,
            Aggregates = aggregates
        };
    }

    public async Task<StreamingReportResult> StreamToNdjsonAsync(
        IAsyncEnumerable<IReadOnlyDictionary<string, object?>> rowSource,
        Stream outputStream,
        CancellationToken cancellationToken = default)
    {
        var startTime = DateTime.UtcNow;
        var aggregates = new Dictionary<string, RunningColumnAggregate>(StringComparer.OrdinalIgnoreCase);
        long rowCount = 0;

        await using var buffer = new DiskSpillRowBuffer(_maxMemoryRows);
        await using var writer = new StreamWriter(outputStream, Encoding.UTF8, bufferSize: 64 * 1024, leaveOpen: true);

        await foreach (var row in rowSource.WithCancellation(cancellationToken))
        {
            rowCount++;
            await buffer.AddRowAsync(row, cancellationToken);

            if (rowCount % 1000 == 0)
            {
                await _governor.EnforceCeilingAsync(cancellationToken);
            }
        }

        // Stream from buffer to NDJSON lines
        await foreach (var row in buffer.ReadAllAsync(cancellationToken))
        {
            foreach (var (key, val) in row)
            {
                if (!aggregates.TryGetValue(key, out var agg))
                {
                    agg = new RunningColumnAggregate { ColumnName = key };
                    aggregates[key] = agg;
                }
                agg.Accumulate(val);
            }

            var line = JsonSerializer.Serialize(row);
            await writer.WriteLineAsync(line);
        }

        await writer.FlushAsync(cancellationToken);

        return new StreamingReportResult
        {
            TotalRowsProcessed = rowCount,
            SpilledRowsCount = buffer.SpilledRowCount,
            PeakAllocatedMemoryBytes = _governor.PeakAllocatedBytes,
            ElapsedDuration = DateTime.UtcNow - startTime,
            Aggregates = aggregates
        };
    }

    private static string EscapeCsvField(string field)
    {
        if (field.Contains(',') || field.Contains('"') || field.Contains('\n') || field.Contains('\r'))
        {
            return $"\"{field.Replace("\"", "\"\"")}\"";
        }
        return field;
    }
}
