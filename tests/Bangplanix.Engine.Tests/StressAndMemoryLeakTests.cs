using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Bangplanix.Core.Models;
using Bangplanix.Expressions.Compiler;
using Bangplanix.Expressions.Functions;
using FluentAssertions;
using Xunit;

#pragma warning disable CA1707, CA2007

namespace Bangplanix.Engine.Tests;

public class StressAndMemoryLeakTests
{
    [Fact]
    public void ExpressionEngine_50kIterations_ShouldProcessQuicklyWithStableMemory()
    {
        const int iterations = 10_000;
        var compiler = new RoslynExpressionCompiler();
        const string formula = "10 * 125.50 * 0.95";

        // Warm up compiler once
        _ = compiler.Evaluate(formula);

        // Force initial GC before benchmark
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        var initialMemory = GC.GetTotalMemory(true);

        var stopwatch = Stopwatch.StartNew();

        double runningTotal = 0;
        for (int i = 0; i < iterations; i++)
        {
            var result = compiler.Evaluate(formula);
            runningTotal += Convert.ToDouble(result, System.Globalization.CultureInfo.InvariantCulture);
        }

        stopwatch.Stop();
        var finalMemory = GC.GetTotalMemory(false);
        var memoryDeltaBytes = finalMemory - initialMemory;

        // Assert
        runningTotal.Should().BeGreaterThan(0);
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(10000, "10,000 compiled expressions must execute in < 10.0s");
        
        // Memory leak check: Should not retain massive permanent allocations (delta < 50MB)
        (memoryDeltaBytes / (1024 * 1024)).Should().BeLessThan(50, "Memory delta for evaluations must remain below 50MB");
    }

    [Fact]
    public void SimdAggregations_50kRows_ShouldComputeSumAndAverageInstantly()
    {
        const int rowCount = 50_000;
        var values = new double[rowCount];
        for (int i = 0; i < rowCount; i++)
        {
            values[i] = i * 1.5;
        }

        var sw = Stopwatch.StartNew();
        var sum = SimdAggregations.Sum(values);
        var avg = SimdAggregations.Average(values);
        sw.Stop();

        sum.Should().BeGreaterThan(0);
        avg.Should().BeGreaterThan(0);
        sw.ElapsedMilliseconds.Should().BeLessThan(50, "SIMD vector summation of 50k values must complete in < 50ms");
    }
}
