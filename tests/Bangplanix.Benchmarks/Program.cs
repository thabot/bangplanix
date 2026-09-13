using BenchmarkDotNet.Running;

namespace Bangplanix.Benchmarks;

public class Program
{
    public static void Main(string[] args)
    {
        var switcher = new BenchmarkSwitcher(new[]
        {
            typeof(BpxParserBenchmark),
            typeof(ChartRenderingBenchmarks),
            typeof(MassiveDataStreamingBenchmarks),
            typeof(CryptographicSecurityBenchmarks),
            typeof(ExpressionEvaluationBenchmarks)
        });

        switcher.Run(args);
    }
}
