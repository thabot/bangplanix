using System;
using System.Collections.Generic;
using Bangplanix.Core.Models;

namespace Bangplanix.Engine.Accessibility;

/// <summary>
/// Enriches Table structure elements with PDF/UA compliant header scopes and cell association IDs.
/// </summary>
public static class TableAccessibilityEnhancer
{
    public static PdfStructElement EnhanceTable(
        PdfStructElement tableElement,
        string tablePrefix = "tbl_")
    {
        ArgumentNullException.ThrowIfNull(tableElement);

        if (tableElement.TagType != PdfTagType.Table)
            return tableElement;

        var headerIds = new List<string>();
        int rowIndex = 0;

        foreach (var row in tableElement.Children)
        {
            if (row.TagType != PdfTagType.TableRow)
                continue;

            int colIndex = 0;
            foreach (var cell in row.Children)
            {
                if (cell.TagType == PdfTagType.TableHeader)
                {
                    cell.Scope = TableScopeType.Column;
                    string hId = $"{tablePrefix}th_r{rowIndex}_c{colIndex}";
                    cell.Id = hId;
                    headerIds.Add(hId);
                }
                else if (cell.TagType == PdfTagType.TableData)
                {
                    string dId = $"{tablePrefix}td_r{rowIndex}_c{colIndex}";
                    cell.Id = dId;

                    // Link to corresponding column header if available
                    if (colIndex < headerIds.Count)
                    {
                        cell.Headers.Add(headerIds[colIndex]);
                    }
                }
                colIndex++;
            }
            rowIndex++;
        }

        return tableElement;
    }
}