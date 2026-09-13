using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Bangplanix.Core.Streaming;

namespace Bangplanix.Engine.Streaming;

public class PartitionConfig
{
    public int MaxRowsPerPartition { get; init; } = 100_000;
    public long MaxBytesPerPartition { get; init; } = 50 * 1024 * 1024; // 50MB
    public bool EnableGzipCompression { get; init; }
    public string BaseFileName { get; init; } = "report_export";
    public string OutputDirectory { get; init; } = Path.GetTempPath();
}

public class PartitionFileInfo
{
    public int PartitionIndex { get; set; }
    public string FilePath { get; set; } = string.Empty;
    public long RowCount { get; set; }
    public long FileSizeBytes { get; set; }
    public string Sha256Hash { get; set; } = string.Empty;
}

public class PartitionedExportManifest
{
    public string ExportId { get; set; } = Guid.NewGuid().ToString("N");
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public long TotalRows { get; set; }
    public int PartitionCount { get; set; }
    public List<PartitionFileInfo> Partitions { get; set; } = new();
}

public class PartitionedStreamingExporter
{
    private readonly PartitionConfig _config;

    public PartitionedStreamingExporter(PartitionConfig? config = null)
    {
        _config = config ?? new PartitionConfig();
        if (!Directory.Exists(_config.OutputDirectory))
        {
            Directory.CreateDirectory(_config.OutputDirectory);
        }
    }

    public async Task<PartitionedExportManifest> ExportToPartitionedCsvAsync(
        IAsyncEnumerable<IReadOnlyDictionary<string, object?>> rows,
        IEnumerable<string>? selectColumns = null,
        CancellationToken cancellationToken = default)
    {
        var manifest = new PartitionedExportManifest();
        int partitionIndex = 1;
        long totalRows = 0;
        List<string>? headers = selectColumns?.ToList();

        Stream? currentFileStream = null;
        Stream? currentWriteStream = null;
        StreamWriter? currentWriter = null;
        string currentFilePath = string.Empty;
        long currentPartitionRows = 0;

        async Task CloseCurrentPartitionAsync()
        {
            if (currentWriter != null)
            {
                await currentWriter.FlushAsync(cancellationToken);
                currentWriter.Dispose();
                currentWriter = null;
                currentWriteStream = null;
                currentFileStream = null;

                manifest.Partitions.Add(CreatePartitionFileInfo(partitionIndex - 1, currentFilePath, currentPartitionRows));
            }
        }

        try
        {
            await foreach (var row in rows.WithCancellation(cancellationToken))
            {
                totalRows++;

                if (currentWriter == null || currentPartitionRows >= _config.MaxRowsPerPartition || (currentFileStream != null && currentFileStream.Length >= _config.MaxBytesPerPartition))
                {
                    await CloseCurrentPartitionAsync();

                    // Open next partition
                    var ext = _config.EnableGzipCompression ? ".csv.gz" : ".csv";
                    currentFilePath = Path.Combine(_config.OutputDirectory, $"{_config.BaseFileName}_part_{partitionIndex:D3}{ext}");
                    currentFileStream = new FileStream(currentFilePath, FileMode.Create, FileAccess.ReadWrite, FileShare.None, 64 * 1024, FileOptions.SequentialScan);
                    
                    if (_config.EnableGzipCompression)
                    {
                        currentWriteStream = new GZipStream(currentFileStream, CompressionLevel.Fastest, leaveOpen: false);
                    }
                    else
                    {
                        currentWriteStream = currentFileStream;
                    }

                    currentWriter = new StreamWriter(currentWriteStream, Encoding.UTF8, 64 * 1024, leaveOpen: false);
                    currentPartitionRows = 0;
                    partitionIndex++;

                    // Write CSV Header
                    headers ??= row.Keys.ToList();
                    await currentWriter.WriteLineAsync(string.Join(",", headers.Select(EscapeCsvField)));
                }

                headers ??= row.Keys.ToList();
                var values = new string[headers.Count];
                for (int i = 0; i < headers.Count; i++)
                {
                    row.TryGetValue(headers[i], out var val);
                    values[i] = EscapeCsvField(val?.ToString() ?? string.Empty);
                }

                await currentWriter.WriteLineAsync(string.Join(",", values));
                currentPartitionRows++;
            }

            // Close last partition
            await CloseCurrentPartitionAsync();
        }
        finally
        {
            await CloseCurrentPartitionAsync();
        }

        manifest.TotalRows = totalRows;
        manifest.PartitionCount = manifest.Partitions.Count;

        // Write manifest.json
        var manifestPath = Path.Combine(_config.OutputDirectory, $"{_config.BaseFileName}_manifest.json");
        var manifestJson = JsonSerializer.Serialize(manifest, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(manifestPath, manifestJson, cancellationToken);

        return manifest;
    }

    private static PartitionFileInfo CreatePartitionFileInfo(int partitionIndex, string filePath, long rowCount)
    {
        var fileInfo = new FileInfo(filePath);
        var bytes = File.ReadAllBytes(filePath);
        var hash = Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();

        return new PartitionFileInfo
        {
            PartitionIndex = partitionIndex,
            FilePath = filePath,
            RowCount = rowCount,
            FileSizeBytes = fileInfo.Length,
            Sha256Hash = hash
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
