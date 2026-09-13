using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using Bangplanix.Core.Models;
using Bangplanix.Core.Parser;

namespace Bangplanix.Benchmarks;

[MemoryDiagnoser]
public class BpxParserBenchmark
{
    private string _json = string.Empty;
    private byte[] _utf8Bytes = [];

    [GlobalSetup]
    public void Setup()
    {
        var samplePath = Path.GetFullPath("../../../../../schema/v1/samples/invoice.bpx");
        if (!File.Exists(samplePath))
        {
            samplePath = Path.GetFullPath("../../../../schema/v1/samples/invoice.bpx");
        }
        _json = File.ReadAllText(samplePath);
        _utf8Bytes = File.ReadAllBytes(samplePath);
    }

    [Benchmark(Baseline = true)]
    public ReportDefinition Parse_String()
    {
        return BpxParser.Parse(_json);
    }

    [Benchmark]
    public ReportDefinition Parse_Utf8Span()
    {
        return BpxParser.Parse(_utf8Bytes.AsSpan());
    }
}
