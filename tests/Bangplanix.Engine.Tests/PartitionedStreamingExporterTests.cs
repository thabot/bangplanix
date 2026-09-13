using System.IO.Compression;
using Bangplanix.Engine.Streaming;
using Xunit;

namespace Bangplanix.Engine.Tests;

public class PartitionedStreamingExporterTests
{
    [Fact]
    public async Task PartitionedStreamingExporter_ShouldPartitionPlainCsvFilesAndProduceManifest()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"bpx_export_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);

        try
        {
            var config = new PartitionConfig
            {
                MaxRowsPerPartition = 250, // split every 250 rows
                OutputDirectory = tempDir,
                BaseFileName = "test_sales_partition",
                EnableGzipCompression = false
            };

            var exporter = new PartitionedStreamingExporter(config);

            async IAsyncEnumerable<IReadOnlyDictionary<string, object?>> GenerateRowsAsync(int count)
            {
                for (int i = 1; i <= count; i++)
                {
                    yield return new Dictionary<string, object?>
                    {
                        ["RowId"] = i,
                        ["Customer"] = $"Customer_{i}",
                        ["Amount"] = i * 15.75
                    };
                }
            }

            var manifest = await exporter.ExportToPartitionedCsvAsync(GenerateRowsAsync(600));

            Assert.Equal(600, manifest.TotalRows);
            Assert.Equal(3, manifest.PartitionCount); // 250 + 250 + 100 = 3 partitions
            Assert.Equal(3, manifest.Partitions.Count);

            Assert.Equal(250, manifest.Partitions[0].RowCount);
            Assert.Equal(250, manifest.Partitions[1].RowCount);
            Assert.Equal(100, manifest.Partitions[2].RowCount);

            // Verify file exists and has hash
            foreach (var part in manifest.Partitions)
            {
                Assert.True(File.Exists(part.FilePath));
                Assert.False(string.IsNullOrEmpty(part.Sha256Hash));
            }

            // Verify manifest.json exists
            var manifestPath = Path.Combine(tempDir, "test_sales_partition_manifest.json");
            Assert.True(File.Exists(manifestPath));
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, recursive: true);
            }
        }
    }

    [Fact]
    public async Task PartitionedStreamingExporter_ShouldSupportGzipCompressedPartitions()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"bpx_gzip_export_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);

        try
        {
            var config = new PartitionConfig
            {
                MaxRowsPerPartition = 100,
                OutputDirectory = tempDir,
                BaseFileName = "compressed_export",
                EnableGzipCompression = true
            };

            var exporter = new PartitionedStreamingExporter(config);

            async IAsyncEnumerable<IReadOnlyDictionary<string, object?>> GenerateRowsAsync(int count)
            {
                for (int i = 1; i <= count; i++)
                {
                    yield return new Dictionary<string, object?>
                    {
                        ["Id"] = i,
                        ["Name"] = $"User_{i}",
                        ["Value"] = 100.0
                    };
                }
            }

            var manifest = await exporter.ExportToPartitionedCsvAsync(GenerateRowsAsync(250));

            Assert.Equal(250, manifest.TotalRows);
            Assert.Equal(3, manifest.PartitionCount); // 100 + 100 + 50 = 3 partitions

            // Verify Gzip decompression on partition 0
            var firstPartPath = manifest.Partitions[0].FilePath;
            Assert.EndsWith(".csv.gz", firstPartPath);

            using var fileStream = File.OpenRead(firstPartPath);
            using var gzipStream = new GZipStream(fileStream, CompressionMode.Decompress);
            using var reader = new StreamReader(gzipStream);

            var header = await reader.ReadLineAsync();
            Assert.Equal("Id,Name,Value", header);

            var firstLine = await reader.ReadLineAsync();
            Assert.Equal("1,User_1,100", firstLine);
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, recursive: true);
            }
        }
    }
}
