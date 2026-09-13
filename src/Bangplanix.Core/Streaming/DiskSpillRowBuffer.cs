using System.Text;

namespace Bangplanix.Core.Streaming;

public class DiskSpillRowBuffer : IAsyncDisposable, IDisposable
{
    private readonly int _maxMemoryRows;
    private readonly string _tempFilePath;
    private readonly List<IReadOnlyDictionary<string, object?>> _memoryRows = new();
    private FileStream? _diskStream;
    private BinaryWriter? _writer;
    private long _spilledRowCount;
    private long _totalRowCount;
    private bool _isSpilled;
    private bool _isDisposed;

    // Fast column dictionary cache
    private readonly List<string> _columnNames = new();
    private readonly Dictionary<string, int> _columnIndexMap = new(StringComparer.Ordinal);

    public long TotalRowCount => _totalRowCount;
    public long SpilledRowCount => _spilledRowCount;
    public bool HasSpilledToDisk => _isSpilled;
    public string TempFilePath => _tempFilePath;

    public DiskSpillRowBuffer(int maxMemoryRows = 5000, string? tempDirectory = null)
    {
        _maxMemoryRows = Math.Max(100, maxMemoryRows);
        var dir = tempDirectory ?? Path.GetTempPath();
        _tempFilePath = Path.Combine(dir, $"bpx_spill_{Guid.NewGuid():N}.bin");
    }

    public async Task AddRowAsync(IReadOnlyDictionary<string, object?> row, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_isDisposed, this);

        _totalRowCount++;

        if (!_isSpilled)
        {
            _memoryRows.Add(row);
            if (_memoryRows.Count >= _maxMemoryRows)
            {
                await SpillMemoryToDiskAsync(cancellationToken);
            }
        }
        else
        {
            await WriteRowToDiskAsync(row, cancellationToken);
            _spilledRowCount++;
        }
    }

    public async Task AddRowsAsync(IEnumerable<IReadOnlyDictionary<string, object?>> rows, CancellationToken cancellationToken = default)
    {
        foreach (var row in rows)
        {
            await AddRowAsync(row, cancellationToken);
        }
    }

    private async Task SpillMemoryToDiskAsync(CancellationToken cancellationToken)
    {
        _isSpilled = true;
        _diskStream = new FileStream(
            _tempFilePath,
            FileMode.Create,
            FileAccess.ReadWrite,
            FileShare.None,
            bufferSize: 64 * 1024,
            FileOptions.Asynchronous | FileOptions.SequentialScan);

        _writer = new BinaryWriter(_diskStream, Encoding.UTF8, leaveOpen: true);

        // Write memory rows to disk
        foreach (var row in _memoryRows)
        {
            await WriteRowToDiskAsync(row, cancellationToken);
            _spilledRowCount++;
        }

        _memoryRows.Clear();
        _memoryRows.TrimExcess();
    }

    private Task WriteRowToDiskAsync(IReadOnlyDictionary<string, object?> row, CancellationToken cancellationToken)
    {
        if (_writer == null) return Task.CompletedTask;

        // Write column count
        _writer.Write(row.Count);

        foreach (var (key, value) in row)
        {
            // Register column name in index dictionary
            if (!_columnIndexMap.TryGetValue(key, out var colIdx))
            {
                colIdx = _columnNames.Count;
                _columnNames.Add(key);
                _columnIndexMap[key] = colIdx;
            }

            _writer.Write(colIdx);
            _writer.Write(key); // Also store raw key string for standalone parsing

            WriteValue(_writer, value);
        }

        return Task.CompletedTask;
    }

    private static void WriteValue(BinaryWriter writer, object? val)
    {
        if (val == null || val is DBNull)
        {
            writer.Write((byte)0); // Null
        }
        else if (val is bool b)
        {
            writer.Write((byte)1);
            writer.Write(b);
        }
        else if (val is int i)
        {
            writer.Write((byte)2);
            writer.Write(i);
        }
        else if (val is long l)
        {
            writer.Write((byte)3);
            writer.Write(l);
        }
        else if (val is double d)
        {
            writer.Write((byte)4);
            writer.Write(d);
        }
        else if (val is decimal dec)
        {
            writer.Write((byte)5);
            writer.Write(dec);
        }
        else if (val is DateTime dt)
        {
            writer.Write((byte)6);
            writer.Write(dt.ToBinary());
        }
        else
        {
            writer.Write((byte)7); // String
            writer.Write(val.ToString() ?? string.Empty);
        }
    }

    private static object? ReadValue(BinaryReader reader)
    {
        var typeCode = reader.ReadByte();
        return typeCode switch
        {
            0 => null,
            1 => reader.ReadBoolean(),
            2 => reader.ReadInt32(),
            3 => reader.ReadInt64(),
            4 => reader.ReadDouble(),
            5 => reader.ReadDecimal(),
            6 => DateTime.FromBinary(reader.ReadInt64()),
            7 => reader.ReadString(),
            _ => null
        };
    }

    public async IAsyncEnumerable<IReadOnlyDictionary<string, object?>> ReadAllAsync([System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_isDisposed, this);

        if (!_isSpilled)
        {
            foreach (var row in _memoryRows)
            {
                cancellationToken.ThrowIfCancellationRequested();
                yield return row;
            }
        }
        else
        {
            // Flush any pending disk writes
            if (_writer != null)
            {
                _writer.Flush();
            }

            if (_diskStream != null)
            {
                await _diskStream.FlushAsync(cancellationToken);
                _diskStream.Position = 0;

                using var reader = new BinaryReader(_diskStream, Encoding.UTF8, leaveOpen: true);

                for (long i = 0; i < _totalRowCount; i++)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    if (_diskStream.Position >= _diskStream.Length)
                    {
                        break;
                    }

                    var colCount = reader.ReadInt32();
                    var dict = new Dictionary<string, object?>(colCount);

                    for (int c = 0; c < colCount; c++)
                    {
                        var _ = reader.ReadInt32(); // colIdx
                        var colName = reader.ReadString();
                        var value = ReadValue(reader);
                        dict[colName] = value;
                    }

                    yield return dict;
                }
            }
        }
    }

    public void Dispose()
    {
        if (_isDisposed) return;
        _isDisposed = true;

        try
        {
            _writer?.Dispose();
            _diskStream?.Dispose();
            if (File.Exists(_tempFilePath))
            {
                File.Delete(_tempFilePath);
            }
        }
        catch
        {
            // Suppress cleanup file deletion errors
        }

        _memoryRows.Clear();
        GC.SuppressFinalize(this);
    }

    public async ValueTask DisposeAsync()
    {
        if (_isDisposed) return;
        _isDisposed = true;

        try
        {
            _writer?.Dispose();
            if (_diskStream != null)
            {
                await _diskStream.DisposeAsync();
            }
            if (File.Exists(_tempFilePath))
            {
                File.Delete(_tempFilePath);
            }
        }
        catch
        {
            // Suppress cleanup errors
        }

        _memoryRows.Clear();
        GC.SuppressFinalize(this);
    }
}
