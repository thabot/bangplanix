using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Bangplanix.Core.Models;

namespace Bangplanix.Engine.Visuals.Charts;

public static class ChartDataBinder
{
    public static void Bind(ChartDefinition chart, IReadOnlyList<IDictionary<string, object?>> rows)
    {
        if (chart == null || chart.DataBinding == null || rows == null || rows.Count == 0) return;

        var binding = chart.DataBinding;
        var catField = binding.CategoryField;
        var valField = binding.ValueField;
        var groupField = binding.SeriesGroupField;
        var aggFunc = binding.AggregateFunction;

        if (string.IsNullOrWhiteSpace(catField) || string.IsNullOrWhiteSpace(valField)) return;

        // Distinct ordered categories
        var categories = rows
            .Select(r => r.TryGetValue(catField, out var cVal) ? cVal?.ToString() ?? string.Empty : string.Empty)
            .Where(c => !string.IsNullOrEmpty(c))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (categories.Count == 0) return;
        chart.Categories = categories;

        // Group by series if seriesGroupField is specified
        if (!string.IsNullOrWhiteSpace(groupField))
        {
            var seriesGroups = rows
                .GroupBy(r => r.TryGetValue(groupField, out var gVal) ? gVal?.ToString() ?? "Default" : "Default", StringComparer.OrdinalIgnoreCase)
                .ToList();

            var newSeriesList = new List<ChartSeriesDefinition>();

            foreach (var sGroup in seriesGroups)
            {
                var seriesName = sGroup.Key;
                var values = new List<double>();

                foreach (var cat in categories)
                {
                    var matchingRows = sGroup
                        .Where(r => r.TryGetValue(catField, out var cVal) && string.Equals(cVal?.ToString(), cat, StringComparison.OrdinalIgnoreCase))
                        .ToList();

                    var aggregatedValue = ComputeAggregate(matchingRows, valField, aggFunc);
                    values.Add(aggregatedValue);
                }

                newSeriesList.Add(new ChartSeriesDefinition
                {
                    Name = seriesName,
                    Values = values,
                    Categories = categories
                });
            }

            chart.Series = newSeriesList;
        }
        else
        {
            // Single Series aggregation
            var values = new List<double>();
            foreach (var cat in categories)
            {
                var matchingRows = rows
                    .Where(r => r.TryGetValue(catField, out var cVal) && string.Equals(cVal?.ToString(), cat, StringComparison.OrdinalIgnoreCase))
                    .ToList();

                var aggregatedValue = ComputeAggregate(matchingRows, valField, aggFunc);
                values.Add(aggregatedValue);
            }

            if (chart.Series.Count == 0)
            {
                chart.Series =
                [
                    new ChartSeriesDefinition
                    {
                        Name = valField,
                        Values = values,
                        Categories = categories
                    }
                ];
            }
            else
            {
                chart.Series[0].Values = values;
                chart.Series[0].Categories = categories;
            }
        }
    }

    private static double ComputeAggregate(List<IDictionary<string, object?>> rows, string valField, ChartAggregateFunction func)
    {
        if (rows.Count == 0) return 0.0;

        var numValues = new List<double>();
        foreach (var r in rows)
        {
            if (r.TryGetValue(valField, out var v) && v != null)
            {
                if (double.TryParse(v.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var d))
                {
                    numValues.Add(d);
                }
            }
        }

        if (numValues.Count == 0) return 0.0;

        return func switch
        {
            ChartAggregateFunction.Sum => numValues.Sum(),
            ChartAggregateFunction.Average => numValues.Average(),
            ChartAggregateFunction.Count => numValues.Count,
            ChartAggregateFunction.Min => numValues.Min(),
            ChartAggregateFunction.Max => numValues.Max(),
            _ => numValues.Sum()
        };
    }
}
