using System;
using System.Collections.Generic;
using System.Linq;
using Bangplanix.Core.Models;

namespace Bangplanix.Engine.Accessibility;

/// <summary>
/// Optimizes logical structure reading order for multi-column and complex report layouts.
/// </summary>
public static class PdfReadingOrderOptimizer
{
    private const double RowYTolerance = 4.0; // 4pt tolerance for elements on the same row

    /// <summary>
    /// Reorders elements within containers according to natural visual flow (Top-to-Bottom, Left-to-Right or Column-First).
    /// </summary>
    public static PdfStructElement OptimizeReadingOrder(PdfStructElement root, double pageWidth = 595.28, int columns = 1)
    {
        ArgumentNullException.ThrowIfNull(root);

        if (root.Children.Count == 0)
            return root;

        if (columns > 1)
        {
            // Multi-column sorting: partition elements into columns by X position, then sort by Y within each column
            double colWidth = pageWidth / columns;
            var columnBuckets = new List<List<PdfStructElement>>();
            for (int i = 0; i < columns; i++)
            {
                columnBuckets.Add([]);
            }

            foreach (var child in root.Children)
            {
                int colIdx = (int)Math.Clamp(Math.Floor(child.X / colWidth), 0, columns - 1);
                columnBuckets[colIdx].Add(child);
            }

            var optimizedChildren = new List<PdfStructElement>();
            for (int i = 0; i < columns; i++)
            {
                var sortedCol = columnBuckets[i]
                    .OrderBy(e => e.Y)
                    .ThenBy(e => e.X)
                    .ToList();
                optimizedChildren.AddRange(sortedCol);
            }

            root.Children = optimizedChildren;
        }
        else
        {
            // Single column flow: group by Y within tolerance, then sort by X
            root.Children = root.Children
                .OrderBy(e => Math.Round(e.Y / RowYTolerance) * RowYTolerance)
                .ThenBy(e => e.X)
                .ToList();
        }

        // Recursively optimize children
        foreach (var child in root.Children)
        {
            OptimizeReadingOrder(child, pageWidth, columns);
        }

        return root;
    }
}