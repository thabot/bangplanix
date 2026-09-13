using System.Text;
using Bangplanix.Core.Bursting;
using Bangplanix.Core.Licensing;
using Bangplanix.Engine.Bursting;
using Bangplanix.Engine.Licensing;
using BenchmarkDotNet.Attributes;

namespace Bangplanix.Benchmarks;

[MemoryDiagnoser]
[ShortRunJob]
public class FinalReleaseBenchmarkSuite
{
    private byte[] _payload = null!;
    private byte[] _privateKey = null!;
    private CommercialLicenseEnforcer _licenseEnforcer = null!;
    private string _signedToken = null!;
    private CronExpressionParser _cronParser = null!;

    [GlobalSetup]
    public void Setup()
    {
        _privateKey = "bangplanix_master_pub_key_2026_ed25519_dilithium"u8.ToArray();
        _payload = Encoding.UTF8.GetBytes("{\"reportId\": \"RPT-1001\", \"tenant\": \"bangplanix-prod\"}");
        _licenseEnforcer = new CommercialLicenseEnforcer(publicKeyBytes: _privateKey);

        var license = new LicensePayload
        {
            LicenseId = "LIC-BENCH-001",
            CustomerName = "Enterprise Benchmark",
            Tier = LicenseTier.Enterprise,
            ExpiresAtUtc = DateTime.UtcNow.AddYears(1)
        };
        _signedToken = CommercialLicenseEnforcer.GenerateSignedToken(license, _privateKey);
        _cronParser = new CronExpressionParser("*/15 8-18 * * 1-5");
    }

    [Benchmark(Description = "Cryptographic Ed25519 License Validation")]
    public LicenseValidationResult ValidateLicenseToken()
    {
        return _licenseEnforcer.ApplyLicenseToken(_signedToken, currentHostCores: 8);
    }

    [Benchmark(Description = "Zero-Alloc Cron Expression Next Occurrence Calc")]
    public DateTime? CalculateNextCronOccurrence()
    {
        return _cronParser.GetNextOccurrenceUtc(DateTime.UtcNow);
    }

    [Benchmark(Description = "Recipient Dynamic Password Generation & Salt Binding")]
    public string GenerateRecipientPassword()
    {
        var policy = new RecipientSecurityPolicy
        {
            PasswordPattern = "TH-{NationalId:Last4}-{Year}",
            FallbackPassword = "Default@2026"
        };
        var row = new Dictionary<string, object?>
        {
            ["NationalId"] = "1100200345678",
            ["Year"] = "2026"
        };
        return RecipientPasswordEncryptionEngine.GenerateRecipientPassword(policy, row);
    }
}
