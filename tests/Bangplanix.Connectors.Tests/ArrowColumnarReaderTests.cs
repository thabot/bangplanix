using Bangplanix.Connectors.BigData;
using Xunit;

namespace Bangplanix.Connectors.Tests;

public class ArrowColumnarReaderTests
{
    [Fact]
    public void ArrowColumnarReaderShouldSerializeAndDeserializeRecordBatch()
    {
        var batch = new ArrowRecordBatch { RowCount = 3 };

        var idCol = new ArrowColumnVector
        {
            Definition = new ArrowColumnDefinition { Name = "id", DataType = ArrowDataType.Int32 }
        };
        idCol.Values.AddRange([101, 102, 103]);

        var nameCol = new ArrowColumnVector
        {
            Definition = new ArrowColumnDefinition { Name = "product_name", DataType = ArrowDataType.String }
        };
        nameCol.Values.AddRange(["Laptop Pro", "Wireless Mouse", "4K Monitor"]);

        var priceCol = new ArrowColumnVector
        {
            Definition = new ArrowColumnDefinition { Name = "price", DataType = ArrowDataType.Decimal }
        };
        priceCol.Values.AddRange([45000.50m, 890.00m, 12500.00m]);

        batch.Columns.Add(idCol);
        batch.Columns.Add(nameCol);
        batch.Columns.Add(priceCol);

        // Serialize to binary buffer
        var buffer = ArrowColumnarReader.SerializeRecordBatch(batch);
        Assert.NotNull(buffer);
        Assert.True(buffer.Length > 0);

        // Deserialize from binary buffer
        var deserialized = ArrowColumnarReader.DeserializeRecordBatch(buffer);
        Assert.Equal(3, deserialized.RowCount);
        Assert.Equal(3, deserialized.Columns.Count);

        var rows = deserialized.ToRows();
        Assert.Equal(3, rows.Count);
        Assert.Equal(101, rows[0]["id"]);
        Assert.Equal("Laptop Pro", rows[0]["product_name"]);
        Assert.Equal(45000.50m, rows[0]["price"]);
        Assert.Equal(102, rows[1]["id"]);
        Assert.Equal("Wireless Mouse", rows[1]["product_name"]);
    }
}
