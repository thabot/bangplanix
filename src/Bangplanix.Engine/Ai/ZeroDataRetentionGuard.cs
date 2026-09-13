using System.Text.Json;
using System.Text.RegularExpressions;

namespace Bangplanix.Engine.Ai;

/// <summary>
/// Column metadata descriptor with zero customer row data.
/// </summary>
public sealed record ColumnSchemaDescriptor(string ColumnName, string DataType, bool IsNullable = true);

/// <summary>
/// Table schema descriptor containing only structural metadata.
/// </summary>
public sealed class TableSchemaDescriptor
{
    public string TableName { get; set; } = string.Empty;
    public List<ColumnSchemaDescriptor> Columns { get; set; } = new();
}

/// <summary>
/// Zero Data Retention (ZDR) Guard that strips all customer row data and extracts only structural metadata.
/// </summary>
public sealed class ZeroDataRetentionGuard
{
    /// <summary>
    /// Extracts safe schema-only metadata from a dataset dictionary or JSON, discarding all row values.
    /// </summary>
    public static List<TableSchemaDescriptor> ExtractSchemaOnly(IEnumerable<KeyValuePair<string, IEnumerable<IDictionary<string, object?>>>> datasets)
    {
        var result = new List<TableSchemaDescriptor>();

        foreach (var (tableName, rows) in datasets)
        {
            var tableDescriptor = new TableSchemaDescriptor { TableName = tableName };
            var firstRow = rows.FirstOrDefault();

            if (firstRow != null)
            {
                foreach (var (colName, val) in firstRow)
                {
                    string inferredType = val switch
                    {
                        null => "string",
                        int or long or short => "integer",
                        float or double or decimal => "decimal",
                        bool => "boolean",
                        DateTime or DateTimeOffset => "datetime",
                        _ => "string"
                    };

                    tableDescriptor.Columns.Add(new ColumnSchemaDescriptor(colName, inferredType, val == null));
                }
            }

            result.Add(tableDescriptor);
        }

        return result;
    }

    /// <summary>
    /// Generates a prompt-safe structural context string without any sensitive customer data values.
    /// </summary>
    public static string BuildSafeSchemaPromptContext(IEnumerable<TableSchemaDescriptor> schemas)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("### Available Database Schema (Metadata Only - Zero Customer Data):");

        foreach (var table in schemas)
        {
            sb.AppendLine($"Table: {table.TableName}");
            sb.AppendLine("Columns:");
            foreach (var col in table.Columns)
            {
                sb.AppendLine($"  - {col.ColumnName} ({col.DataType})");
            }
            sb.AppendLine();
        }

        return sb.ToString();
    }
}
