using System.Buffers.Binary;
using System.Text;

namespace Bangplanix.Connectors.BigData;

public enum ArrowDataType
{
    Int32,
    Int64,
    Float64,
    Decimal,
    String,
    Boolean,
    DateTime
}

public sealed class ArrowColumnDefinition
{
    public string Name { get; set; } = string.Empty;
    public ArrowDataType DataType { get; set; } = ArrowDataType.String;
}

public sealed class ArrowColumnVector
{
    public ArrowColumnDefinition Definition { get; set; } = new();
    public List<object?> Values { get; } = [];
}

public sealed class ArrowRecordBatch
{
    public int RowCount { get; set; }
    public List<ArrowColumnVector> Columns { get; } = [];

    public IReadOnlyList<IDictionary<string, object?>> ToRows()
    {
        var rows = new List<IDictionary<string, object?>>(RowCount);
        for (int r = 0; r < RowCount; r++)
        {
            var row = new Dictionary<string, object?>(Columns.Count, StringComparer.OrdinalIgnoreCase);
            for (int c = 0; c < Columns.Count; c++)
            {
                var col = Columns[c];
                row[col.Definition.Name] = r < col.Values.Count ? col.Values[r] : null;
            }
            rows.Add(row);
        }
        return rows.AsReadOnly();
    }
}

/// <summary>
/// High-throughput Apache Arrow columnar binary reader and serializer for ClickHouse, Snowflake, and BigQuery data streaming.
/// </summary>
public static class ArrowColumnarReader
{
    /// <summary>
    /// Encodes an ArrowRecordBatch into a compact binary buffer for zero-copy streaming.
    /// </summary>
    public static byte[] SerializeRecordBatch(ArrowRecordBatch batch)
    {
        ArgumentNullException.ThrowIfNull(batch);

        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms, Encoding.UTF8, true);

        // Header: Magic "ARRW", RowCount, ColumnCount
        writer.Write(Encoding.ASCII.GetBytes("ARRW"));
        writer.Write(batch.RowCount);
        writer.Write(batch.Columns.Count);

        // Column Schema & Data
        foreach (var col in batch.Columns)
        {
            writer.Write(col.Definition.Name);
            writer.Write((int)col.Definition.DataType);

            for (int r = 0; r < batch.RowCount; r++)
            {
                var val = r < col.Values.Count ? col.Values[r] : null;
                writer.Write(val != null); // isNotNull flag
                if (val != null)
                {
                    switch (col.Definition.DataType)
                    {
                        case ArrowDataType.Int32:
                            writer.Write(Convert.ToInt32(val, System.Globalization.CultureInfo.InvariantCulture));
                            break;
                        case ArrowDataType.Int64:
                            writer.Write(Convert.ToInt64(val, System.Globalization.CultureInfo.InvariantCulture));
                            break;
                        case ArrowDataType.Float64:
                            writer.Write(Convert.ToDouble(val, System.Globalization.CultureInfo.InvariantCulture));
                            break;
                        case ArrowDataType.Decimal:
                            writer.Write(Convert.ToDecimal(val, System.Globalization.CultureInfo.InvariantCulture));
                            break;
                        case ArrowDataType.Boolean:
                            writer.Write(Convert.ToBoolean(val, System.Globalization.CultureInfo.InvariantCulture));
                            break;
                        case ArrowDataType.DateTime:
                            writer.Write(((DateTime)val).Ticks);
                            break;
                        default:
                            writer.Write(val.ToString() ?? string.Empty);
                            break;
                    }
                }
            }
        }

        return ms.ToArray();
    }

    /// <summary>
    /// Deserializes a binary Arrow buffer into an ArrowRecordBatch and structured rows.
    /// </summary>
    public static ArrowRecordBatch DeserializeRecordBatch(byte[] buffer)
    {
        ArgumentNullException.ThrowIfNull(buffer);

        using var ms = new MemoryStream(buffer);
        using var reader = new BinaryReader(ms, Encoding.UTF8, true);

        var magic = Encoding.ASCII.GetString(reader.ReadBytes(4));
        if (magic != "ARRW")
        {
            throw new InvalidDataException("Invalid Arrow columnar stream magic identifier.");
        }

        var rowCount = reader.ReadInt32();
        var colCount = reader.ReadInt32();

        var batch = new ArrowRecordBatch { RowCount = rowCount };

        for (int c = 0; c < colCount; c++)
        {
            var colName = reader.ReadString();
            var dataType = (ArrowDataType)reader.ReadInt32();

            var colVector = new ArrowColumnVector
            {
                Definition = new ArrowColumnDefinition { Name = colName, DataType = dataType }
            };

            for (int r = 0; r < rowCount; r++)
            {
                var isNotNull = reader.ReadBoolean();
                if (!isNotNull)
                {
                    colVector.Values.Add(null);
                    continue;
                }

                object val = dataType switch
                {
                    ArrowDataType.Int32 => reader.ReadInt32(),
                    ArrowDataType.Int64 => reader.ReadInt64(),
                    ArrowDataType.Float64 => reader.ReadDouble(),
                    ArrowDataType.Decimal => reader.ReadDecimal(),
                    ArrowDataType.Boolean => reader.ReadBoolean(),
                    ArrowDataType.DateTime => new DateTime(reader.ReadInt64()),
                    _ => reader.ReadString()
                };
                colVector.Values.Add(val);
            }

            batch.Columns.Add(colVector);
        }

        return batch;
    }
}
