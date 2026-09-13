using System.Numerics;
using System.Runtime.CompilerServices;

namespace Bangplanix.Expressions.Functions;

public static class SimdAggregations
{
    [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
    public static double Sum(ReadOnlySpan<double> values)
    {
        if (values.IsEmpty) return 0.0;

        int i = 0;
        int vectorSize = Vector<double>.Count;
        var vectorSum = Vector<double>.Zero;

        if (Vector.IsHardwareAccelerated && values.Length >= vectorSize)
        {
            int limit = values.Length - vectorSize;
            while (i <= limit)
            {
                var v = new Vector<double>(values.Slice(i, vectorSize));
                vectorSum += v;
                i += vectorSize;
            }
        }

        double total = Vector.Dot(vectorSum, Vector<double>.One);

        // Scalar remainder
        while (i < values.Length)
        {
            total += values[i];
            i++;
        }

        return total;
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
    public static double Average(ReadOnlySpan<double> values)
    {
        if (values.IsEmpty) return 0.0;
        return Sum(values) / values.Length;
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
    public static double Min(ReadOnlySpan<double> values)
    {
        if (values.IsEmpty) return 0.0;

        int i = 0;
        int vectorSize = Vector<double>.Count;
        double minVal = values[0];

        if (Vector.IsHardwareAccelerated && values.Length >= vectorSize)
        {
            var vectorMin = new Vector<double>(values.Slice(0, vectorSize));
            i = vectorSize;
            int limit = values.Length - vectorSize;
            while (i <= limit)
            {
                var v = new Vector<double>(values.Slice(i, vectorSize));
                vectorMin = Vector.Min(vectorMin, v);
                i += vectorSize;
            }

            for (int k = 0; k < vectorSize; k++)
            {
                if (vectorMin[k] < minVal) minVal = vectorMin[k];
            }
        }

        while (i < values.Length)
        {
            if (values[i] < minVal) minVal = values[i];
            i++;
        }

        return minVal;
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
    public static double Max(ReadOnlySpan<double> values)
    {
        if (values.IsEmpty) return 0.0;

        int i = 0;
        int vectorSize = Vector<double>.Count;
        double maxVal = values[0];

        if (Vector.IsHardwareAccelerated && values.Length >= vectorSize)
        {
            var vectorMax = new Vector<double>(values.Slice(0, vectorSize));
            i = vectorSize;
            int limit = values.Length - vectorSize;
            while (i <= limit)
            {
                var v = new Vector<double>(values.Slice(i, vectorSize));
                vectorMax = Vector.Max(vectorMax, v);
                i += vectorSize;
            }

            for (int k = 0; k < vectorSize; k++)
            {
                if (vectorMax[k] > maxVal) maxVal = vectorMax[k];
            }
        }

        while (i < values.Length)
        {
            if (values[i] > maxVal) maxVal = values[i];
            i++;
        }

        return maxVal;
    }
}
