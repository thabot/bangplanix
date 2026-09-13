using Bangplanix.Core.Streaming;
using Bangplanix.Engine.Streaming;
using Xunit;

namespace Bangplanix.Engine.Tests;

public class MassiveDataStressAndMemoryCeilingTests
{
    [Fact]
    public async Task MassiveDataStreaming_100kRowsStreaming_ShouldNeverExceedMemoryCeiling()
    {
        // Enforce strict 64MB memory ceiling
        var governor = new MemoryCeilingGovernor(maxAllowedBytes: 64 * 1024 * 1024);
        var streamingEngine = new MassiveDataStreamingEngine(maxAllowedMemoryBytes: 64 * 1024 * 1024, maxMemoryRows: 2000);

        async IAsyncEnumerable<IReadOnlyDictionary<string, object?>> GenerateMassiveRowsAsync(int count)
        {
            for (int i = 1; i <= count; i++)
            {
                yield return new Dictionary<string, object?>
                {
                    ["TransactionId"] = i,
                    ["Account"] = $"ACC-{(i % 1000):D6}",
                    ["Amount"] = (double)(i * 0.25),
                    ["Fee"] = (double)(i % 50),
                    ["Status"] = i % 2 == 0 ? "SETTLED" : "PENDING"
                };
            }
        }

        using var outputStream = new MemoryStream();
        var result = await streamingEngine.StreamToCsvAsync(GenerateMassiveRowsAsync(100_000), outputStream);

        Assert.Equal(100_000, result.TotalRowsProcessed);
        Assert.True(result.SpilledRowsCount > 0);
        Assert.True(outputStream.Length > 0);

        // Verify that memory governor recorded resident memory within bounds
        var currentMemory = GC.GetTotalMemory(forceFullCollection: false);
        // Memory should be well under 128MB resident limit
        Assert.True(currentMemory < 128 * 1024 * 1024);

        // Verify accurate running statistics across 100,000 rows
        var amountAgg = result.Aggregates["Amount"];
        Assert.Equal(100_000, amountAgg.Count);
        Assert.Equal(0.25, amountAgg.Min);
        Assert.Equal(25_000.0, amountAgg.Max);
        Assert.Equal(12_500.125, amountAgg.Mean, precision: 2);
    }

    [Fact]
    public async Task DiskSpillRowBuffer_HighThroughputChunkStress_ShouldMaintainZeroLossIntegrity()
    {
        await using var buffer = new DiskSpillRowBuffer(maxMemoryRows: 1000);

        const int totalRows = 25_000;
        for (int i = 1; i <= totalRows; i++)
        {
            await buffer.AddRowAsync(new Dictionary<string, object?>
            {
                ["Id"] = i,
                ["Code"] = $"SKU-{i:D5}",
                ["Price"] = (double)(i * 2.0),
                ["Active"] = true
            });
        }

        Assert.Equal(totalRows, buffer.TotalRowCount);
        Assert.True(buffer.HasSpilledToDisk);

        long enumeratedRows = 0;
        double priceSum = 0;
        await foreach (var row in buffer.ReadAllAsync())
        {
            enumeratedRows++;
            priceSum += Convert.ToDouble(row["Price"]);
        }

        Assert.Equal(totalRows, enumeratedRows);
        var expectedPriceSum = (25_000.0 * 25_001.0 / 2.0) * 2.0;
        Assert.Equal(expectedPriceSum, priceSum, precision: 1);
    }
}
