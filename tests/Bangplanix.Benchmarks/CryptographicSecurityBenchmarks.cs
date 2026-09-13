using System.Text;
using BenchmarkDotNet.Attributes;
using Bangplanix.Core.Audit;
using Bangplanix.Engine.Security;

namespace Bangplanix.Benchmarks;

[MemoryDiagnoser]
public class CryptographicSecurityBenchmarks
{
    private byte[] _dummyPdf = null!;
    private List<byte[]> _pages = null!;
    private AuditEvent _sampleEvent = null!;

    [GlobalSetup]
    public void Setup()
    {
        _dummyPdf = Encoding.UTF8.GetBytes("%PDF-1.7\n1 0 obj\n<< /Type /Catalog >>\nendobj\n%%EOF");
        _pages = new List<byte[]>
        {
            Encoding.UTF8.GetBytes("PAGE_1_FINANCIAL_SUMMARY"),
            Encoding.UTF8.GetBytes("PAGE_2_TRANSACTIONS"),
            Encoding.UTF8.GetBytes("PAGE_3_AUDIT_REPORT"),
            Encoding.UTF8.GetBytes("PAGE_4_DISCLOSURES")
        };

        _sampleEvent = new AuditEvent
        {
            EventId = Guid.NewGuid(),
            Timestamp = DateTimeOffset.UtcNow,
            TenantId = "tenant-enterprise-bank",
            UserId = "user-auditor-42",
            Action = "RenderConfidentialPayrollReport",
            ResourceName = "payroll_q4_2026.bpx",
            ClientIp = "192.168.10.55",
            Details = new Dictionary<string, object?>
            {
                ["Rows"] = 5000,
                ["ExecutionMs"] = 12.5,
                ["Format"] = "PDF/A-3b"
            }
        };
    }

    [Benchmark(Baseline = true)]
    public byte[] PAdES_SignPdf()
    {
        return PdfDigitalSigner.SignPdf(_dummyPdf, new PdfSigningOptions
        {
            SignerName = "Benchmark Signer",
            Reason = "Performance Verification"
        });
    }

    [Benchmark]
    public (string MerkleRoot, List<string> LeafHashes) MerkleTree_BuildRoot()
    {
        return DocumentTamperSealEngine.BuildMerkleTree(_pages);
    }

    [Benchmark]
    public string SIEM_Format_CEF()
    {
        return AuditFormatter.ToCef(_sampleEvent);
    }

    [Benchmark]
    public string SIEM_Format_ECS_Json()
    {
        return AuditFormatter.ToEcsJson(_sampleEvent);
    }
}
