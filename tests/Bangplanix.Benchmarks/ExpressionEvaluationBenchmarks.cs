using BenchmarkDotNet.Attributes;
using Bangplanix.Expressions.Compiler;

namespace Bangplanix.Benchmarks;

[MemoryDiagnoser]
public class ExpressionEvaluationBenchmarks
{
    private RoslynExpressionCompiler _compiler = null!;

    [GlobalSetup]
    public void Setup()
    {
        _compiler = new RoslynExpressionCompiler();
        // Warm up / pre-compile expressions into cache
        _compiler.Evaluate("1250.00 * 8 * (1.0 - 0.15) * (1.0 + 0.07)");
        _compiler.Evaluate("8 > 5 ? \"Bulk Discount Applied\" : \"Standard Pricing\"");
        _compiler.Evaluate("$\"Page 4 of 10\"");
    }

    [Benchmark(Baseline = true)]
    public object? Evaluate_ArithmeticFormula()
    {
        return _compiler.Evaluate("1250.00 * 8 * (1.0 - 0.15) * (1.0 + 0.07)");
    }

    [Benchmark]
    public object? Evaluate_TernaryCondition()
    {
        return _compiler.Evaluate("8 > 5 ? \"Bulk Discount Applied\" : \"Standard Pricing\"");
    }

    [Benchmark]
    public object? Evaluate_StringInterpolation()
    {
        return _compiler.Evaluate("$\"Page 4 of 10\"");
    }
}
