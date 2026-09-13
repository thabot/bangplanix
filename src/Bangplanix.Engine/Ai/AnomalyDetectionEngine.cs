namespace Bangplanix.Engine.Ai;

/// <summary>
/// Severity level of a detected statistical anomaly.
/// </summary>
public enum AnomalySeverity
{
    Low,
    Medium,
    High,
    Critical
}

/// <summary>
/// Represents a single detected outlier/anomaly point in a report metric.
/// </summary>
public sealed record AnomalyPoint(int RowIndex, double Value, double ZScore, AnomalySeverity Severity, string Description);

/// <summary>
/// Result of an anomaly detection analysis on a dataset metric column.
/// </summary>
public sealed class AnomalyAnalysisResult
{
    public string ColumnName { get; set; } = string.Empty;
    public int TotalSamples { get; set; }
    public double Mean { get; set; }
    public double StdDev { get; set; }
    public double Median { get; set; }
    public double Q1 { get; set; }
    public double Q3 { get; set; }
    public double Iqr => Q3 - Q1;
    public List<AnomalyPoint> Anomalies { get; set; } = new();
    public bool HasAnomalies => Anomalies.Count > 0;
}

/// <summary>
/// Statistical Anomaly Detection Engine using IQR & Z-Score algorithms.
/// </summary>
public sealed class AnomalyDetectionEngine
{
    public double ZScoreThreshold { get; set; } = 2.5;

    /// <summary>
    /// Analyzes a list of numeric values and detects statistical outliers.
    /// </summary>
    public AnomalyAnalysisResult AnalyzeColumn(string columnName, IReadOnlyList<double> values)
    {
        if (values == null || values.Count == 0)
        {
            return new AnomalyAnalysisResult { ColumnName = columnName, TotalSamples = 0 };
        }

        int n = values.Count;
        double mean = values.Average();
        double sumSquares = values.Sum(v => Math.Pow(v - mean, 2));
        double stdDev = n > 1 ? Math.Sqrt(sumSquares / (n - 1)) : 0.0;

        var sorted = values.OrderBy(v => v).ToList();
        double median = GetPercentile(sorted, 0.50);
        double q1 = GetPercentile(sorted, 0.25);
        double q3 = GetPercentile(sorted, 0.75);
        double iqr = q3 - q1;

        double lowerFence = q1 - (1.5 * iqr);
        double upperFence = q3 + (1.5 * iqr);
        double severeLowerFence = q1 - (3.0 * iqr);
        double severeUpperFence = q3 + (3.0 * iqr);

        var anomalies = new List<AnomalyPoint>();

        for (int i = 0; i < n; i++)
        {
            double val = values[i];
            double zScore = stdDev > 0 ? Math.Abs((val - mean) / stdDev) : 0;

            bool isOutlier = val < lowerFence || val > upperFence || zScore >= ZScoreThreshold;

            if (isOutlier)
            {
                var severity = AnomalySeverity.Medium;
                if (val < severeLowerFence || val > severeUpperFence || zScore >= 3.5)
                {
                    severity = AnomalySeverity.Critical;
                }
                else if (zScore >= 3.0)
                {
                    severity = AnomalySeverity.High;
                }

                string direction = val > mean ? "Spike (+)" : "Drop (-)";
                string desc = $"{direction} value {val:N2} exceeds normal range [{lowerFence:N2} - {upperFence:N2}] (Z-Score: {zScore:F2})";

                anomalies.Add(new AnomalyPoint(i, val, zScore, severity, desc));
            }
        }

        return new AnomalyAnalysisResult
        {
            ColumnName = columnName,
            TotalSamples = n,
            Mean = Math.Round(mean, 2),
            StdDev = Math.Round(stdDev, 2),
            Median = Math.Round(median, 2),
            Q1 = Math.Round(q1, 2),
            Q3 = Math.Round(q3, 2),
            Anomalies = anomalies
        };
    }

    private static double GetPercentile(List<double> sortedValues, double percentile)
    {
        int n = sortedValues.Count;
        if (n == 0) return 0;
        if (n == 1) return sortedValues[0];

        double index = percentile * (n - 1);
        int lower = (int)Math.Floor(index);
        int upper = (int)Math.Ceiling(index);

        if (lower == upper) return sortedValues[lower];
        double fraction = index - lower;
        return sortedValues[lower] + (fraction * (sortedValues[upper] - sortedValues[lower]));
    }
}
