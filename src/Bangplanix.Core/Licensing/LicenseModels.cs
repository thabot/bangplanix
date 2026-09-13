namespace Bangplanix.Core.Licensing;

/// <summary>
/// Available Bangplanix commercial licensing tiers.
/// </summary>
public enum LicenseTier
{
    Community = 0,      // Free, Watermarked, Max 2 CPU cores, Community support
    Professional = 1,   // $49/mo, Unwatermarked, Up to 8 CPU cores, Standard SLA
    Enterprise = 2,     // $499/mo, Unwatermarked, Unlimited cores, High-availability SLA, AI Suite, True Vector Redaction
    OEMSovereign = 3,   // Custom, Unlimited cores, Air-gapped offline validation, Quantum-Safe cryptography, Source redistribution rights
    OEM = 3             // Alias for OEMSovereign
}

/// <summary>
/// Digital payload containing customer entitlements, core limits, and validity period.
/// </summary>
public sealed class LicensePayload
{
    public string LicenseId { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerEmail { get; set; } = string.Empty;
    public LicenseTier Tier { get; set; } = LicenseTier.Community;
    public int MaxAllowedCores { get; set; } = 2;
    public int MaxTenants { get; set; } = 1;
    public bool EnableAiSuite { get; set; }
    public bool EnableReportBursting { get; set; }
    public bool EnableTrueVectorRedaction { get; set; }
    public bool EnableWatermarking { get; set; } = true;
    public DateTime IssuedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAtUtc { get; set; } = DateTime.MaxValue;
    public string Algorithm { get; set; } = "Ed25519"; // Ed25519, ML-DSA-65, Hybrid-Dilithium
    public Dictionary<string, string> CustomAttributes { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

/// <summary>
/// Outcome of cryptographic license validation against the host runtime environment.
/// </summary>
public sealed class LicenseValidationResult
{
    public bool IsValid { get; set; }
    public LicenseTier ActiveTier { get; set; } = LicenseTier.Community;
    public LicensePayload? Payload { get; set; }
    public string? StatusMessage { get; set; }
    public bool IsExpired { get; set; }
    public bool CoreLimitExceeded { get; set; }
    public int HostCpuCores { get; set; }
    public DateTime ValidatedAtUtc { get; set; } = DateTime.UtcNow;
}

public sealed class LicenseInfo
{
    public string LicenseKey { get; set; } = string.Empty;
    public string LicensedTo { get; set; } = "Community User";
    public LicenseTier Tier { get; set; } = LicenseTier.Community;
    public DateTime? ExpiryDateUtc { get; set; }
    public bool IsValid { get; set; } = true;
    public string? DigitalSignature { get; set; }
}

public interface ILicenseEnforcer
{
    LicenseInfo GetCurrentLicense();
    bool ValidateLicense(string licenseKey);
    bool IsFeatureAllowed(string featureCode);
}
