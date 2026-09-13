using Bangplanix.Core.Streaming;
using Bangplanix.Engine.Streaming;
using Xunit;

namespace Bangplanix.Engine.Tests;

public class MassiveDataStreamingEngineTests
{
    [Fact]
    public async Task DiskSpillRowBuffer_ShouldBufferAndSpillCorrectly()
    {
        // Max 500 rows in memory before spilling to disk
        await using var buffer = new DiskSpillRowBuffer(maxMemoryRows: 500);

        var rowsToGenerate = 3000;
        for (int i = 0; i < rowsToGenerate; i++)
        {
            var row = new Dictionary<string, object?>
            {
                ["TransactionId"] = $"TX-{i:D6}",
                ["Amount"] = (double)(i * 10.5),
                ["IsActive"] = i % 2 == 0,
                ["Timestamp"] = DateTime.UtcNow
            };
            await buffer.AddRowAsync(row);
        }

        Assert.Equal(3000, buffer.TotalRowCount);
        Assert.True(buffer.HasSpilledToDisk);
        Assert.Equal(3000, buffer.SpilledRowCount);

        // Read all rows back and verify consistency
        long readCount = 0;
        double sumAmount = 0;
        await foreach (var row in buffer.ReadAllAsync())
        {
            readCount++;
            var amount = Convert.ToDouble(row["Amount"]);
            sumAmount += amount;
            Assert.StartsWith("TX-", row["TransactionId"]?.ToString());
        }

        Assert.Equal(3000, readCount);
        var expectedSum = (2999.0 * 3000.0 / 2.0) * 10.5;
        Assert.Equal(expectedSum, sumAmount, precision: 2);
    }

    [Fact]
    public async Task MassiveDataStreamingEngine_ShouldStreamLargeDatasetWithRunningAggregatesUnderMemoryCeiling()
    {
        var engine = new MassiveDataStreamingEngine(maxAllowedMemoryBytes: 64 * 1024 * 1024, maxMemoryRows: 1000);

        async IAsyncEnumerable<IReadOnlyDictionary<string, object?>> GenerateRowsAsync(int count)
        {
            for (int i = 1; i <= count; i++)
            {
                yield return new Dictionary<string, object?>
                {
                    ["Id"] = i,
                    ["Category"] = i % 5 == 0 ? "VIP" : "Standard",
                    ["Revenue"] = (double)(i * 100.0),
                    ["Discount"] = (double)(i % 10)
                };
            }
        }

        using var memoryStream = new MemoryStream();
        var result = await engine.StreamToCsvAsync(GenerateRowsAsync(10_000), memoryStream);

        Assert.Equal(10_000, result.TotalRowsProcessed);
        Assert.True(result.SpilledRowsCount > 0);
        Assert.True(memoryStream.Length > 0);

        // Check running aggregates
        Assert.True(result.Aggregates.ContainsKey("Revenue"));
        var revenueAgg = result.Aggregates["Revenue"];
        Assert.Equal(10_000, revenueAgg.Count);
        Assert.Equal(100.0, revenueAgg.Min);
        Assert.Equal(1_000_000.0, revenueAgg.Max);
        Assert.Equal(500_050.0, revenueAgg.Mean, precision: 1);
        Assert.True(revenueAgg.StandardDeviation > 0);

        // Verify CSV content
        memoryStream.Position = 0;
        using var reader = new StreamReader(memoryStream);
        var header = await reader.ReadLineAsync();
        Assert.Equal("Id,Category,Revenue,Discount", header);

        var firstRow = await reader.ReadLineAsync();
        Assert.Equal("1,Standard,100,1", firstRow);
    }

    [Fact]
    public async Task MassiveDataStreamingEngine_ShouldStreamNdjsonCorrectly()
    {
        var engine = new MassiveDataStreamingEngine(maxAllowedMemoryBytes: 32 * 1024 * 1024, maxMemoryRows: 500);

        async IAsyncEnumerable<IReadOnlyDictionary<string, object?>> GenerateRowsAsync(int count)
        {
            for (int i = 1; i <= count; i++)
            {
                yield return new Dictionary<string, object?>
                {
                    ["OrderId"] = i,
                    ["Amount"] = (double)(i * 50.0)
                };
            }
        }

        using var memoryStream = new MemoryStream();
        var result = await engine.StreamToNdjsonAsync(GenerateRowsAsync(2_000), memoryStream);

        Assert.Equal(2_000, result.TotalRowsProcessed);
        Assert.Equal(2_000, result.Aggregates["Amount"].Count);

        memoryStream.Position = 0;
        using var reader = new StreamReader(memoryStream);
        var firstLine = await reader.ReadLineAsync();
        Assert.NotNull(firstLine);
        Assert.Contains("\"OrderId\":1", firstLine);
        Assert.Contains("\"Amount\":50", firstLine);
    }
}
