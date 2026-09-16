using System.Text;
using System.Text.Json;
using Bangplanix.Core.Licensing;

namespace Bangplanix.Engine.Licensing;

/// <summary>
/// Central Commercial OEM & Enterprise License Enforcer.
/// Validates cryptographically signed licenses, enforces core counts, air-gapped validity, and feature entitlement flags.
/// </summary>
public sealed class CommercialLicenseEnforcer
{
    public const string OfficialPublicKeyPem = """
-----BEGIN PUBLIC KEY-----
MFkwEwYHKoZIzj0CAQYIKoZIzj0DAQcDQgAEifypBfJuRuE6r/q2tyBccAUvn+gE
+zb+MXPSdh4GANAqfadgiqBU3BAFa5olWD2DIZF88cBb8kgYskZuHjKkYg==
-----END PUBLIC KEY-----
""";

    private static readonly byte[] DefaultPublicKey = Encoding.UTF8.GetBytes(OfficialPublicKeyPem);
    private readonly ILicenseSignatureVerifier _verifier;
    private readonly byte[] _publicKeyBytes;
    private LicensePayload? _currentPayload;
    private LicenseValidationResult _lastResult;

    public CommercialLicenseEnforcer(ILicenseSignatureVerifier? verifier = null, byte[]? publicKeyBytes = null)
    {
        _verifier = verifier ?? new QuantumReadyLicenseVerifier();
        _publicKeyBytes = publicKeyBytes ?? DefaultPublicKey;
        _lastResult = new LicenseValidationResult
        {
            IsValid = true,
            ActiveTier = LicenseTier.Community,
            StatusMessage = "Operating in Community Edition (Evaluation & Open-Source Tier)."
        };
    }

    public LicenseValidationResult CurrentStatus => _lastResult;

    /// <summary>
    /// Validates a raw license token (base64 payload + dot + base64 signature).
    /// </summary>
    public LicenseValidationResult ApplyLicenseToken(string? licenseToken, int? currentHostCores = null)
    {
        int hostCores = currentHostCores ?? Environment.ProcessorCount;

        if (string.IsNullOrWhiteSpace(licenseToken))
        {
            _lastResult = new LicenseValidationResult
            {
                IsValid = true,
                ActiveTier = LicenseTier.Community,
                HostCpuCores = hostCores,
                StatusMessage = "No commercial license provided. Defaulting to Community Edition."
            };
            return _lastResult;
        }

        string[] parts = licenseToken.Trim().Split('.');
        if (parts.Length != 2)
        {
            _lastResult = new LicenseValidationResult
            {
                IsValid = false,
                ActiveTier = LicenseTier.Community,
                HostCpuCores = hostCores,
                StatusMessage = "Invalid license token format. Expected '<base64Payload>.<base64Signature>'."
            };
            return _lastResult;
        }

        try
        {
            byte[] payloadBytes = Convert.FromBase64String(parts[0]);
            byte[] signatureBytes = Convert.FromBase64String(parts[1]);

            // Cryptographic signature check
            if (!_verifier.VerifySignature(payloadBytes, signatureBytes, _publicKeyBytes))
            {
                _lastResult = new LicenseValidationResult
                {
                    IsValid = false,
                    ActiveTier = LicenseTier.Community,
                    HostCpuCores = hostCores,
                    StatusMessage = "Cryptographic signature mismatch. License token has been tampered with or corrupted."
                };
                return _lastResult;
            }

            string json = Encoding.UTF8.GetString(payloadBytes);
            var payload = JsonSerializer.Deserialize<LicensePayload>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (payload == null)
            {
                _lastResult = new LicenseValidationResult
                {
                    IsValid = false,
                    ActiveTier = LicenseTier.Community,
                    HostCpuCores = hostCores,
                    StatusMessage = "Failed to deserialize license entitlement payload."
                };
                return _lastResult;
            }

            // Expiration check
            if (DateTime.UtcNow > payload.ExpiresAtUtc)
            {
                _lastResult = new LicenseValidationResult
                {
                    IsValid = false,
                    IsExpired = true,
                    ActiveTier = LicenseTier.Community,
                    Payload = payload,
                    HostCpuCores = hostCores,
                    StatusMessage = $"License expired on {payload.ExpiresAtUtc:yyyy-MM-dd HH:mm:ss} UTC."
                };
                return _lastResult;
            }

            // Core quota check (if not unlimited)
            bool coreLimitExceeded = payload.MaxAllowedCores > 0 && hostCores > payload.MaxAllowedCores;
            if (coreLimitExceeded && payload.Tier != LicenseTier.Enterprise && payload.Tier != LicenseTier.OEMSovereign)
            {
                _lastResult = new LicenseValidationResult
                {
                    IsValid = false,
                    CoreLimitExceeded = true,
                    ActiveTier = LicenseTier.Community,
                    Payload = payload,
                    HostCpuCores = hostCores,
                    StatusMessage = $"Core limit exceeded: Host has {hostCores} cores, but license permits up to {payload.MaxAllowedCores} cores."
                };
                return _lastResult;
            }

            _currentPayload = payload;
            _lastResult = new LicenseValidationResult
            {
                IsValid = true,
                ActiveTier = payload.Tier,
                Payload = payload,
                HostCpuCores = hostCores,
                StatusMessage = $"Active {payload.Tier} license granted to {payload.CustomerName} ({payload.CustomerEmail})."
            };

            return _lastResult;
        }
        catch (Exception ex)
        {
            _lastResult = new LicenseValidationResult
            {
                IsValid = false,
                ActiveTier = LicenseTier.Community,
                HostCpuCores = hostCores,
                StatusMessage = $"License validation error: {ex.Message}"
            };
            return _lastResult;
        }
    }

    /// <summary>
    /// Generates a signed license token string using private key.
    /// </summary>
    public static string GenerateSignedToken(LicensePayload payload, byte[] privateKey)
    {
        ArgumentNullException.ThrowIfNull(payload);
        ArgumentNullException.ThrowIfNull(privateKey);

        string json = JsonSerializer.Serialize(payload);
        byte[] payloadBytes = Encoding.UTF8.GetBytes(json);
        byte[] signatureBytes = Ed25519LicenseVerifier.SignPayload(payloadBytes, privateKey);

        string b64Payload = Convert.ToBase64String(payloadBytes);
        string b64Sig = Convert.ToBase64String(signatureBytes);

        return $"{b64Payload}.{b64Sig}";
    }

    /// <summary>
    /// Determines whether output watermarking is mandatory.
    /// </summary>
    public bool RequiresWatermark()
    {
        if (!_lastResult.IsValid) return true;
        if (_lastResult.ActiveTier == LicenseTier.Community) return true;
        return _currentPayload?.EnableWatermarking ?? true;
    }

    /// <summary>
    /// Asserts that a feature is permitted under the active license tier.
    /// </summary>
    public bool IsFeatureAllowed(string featureName)
    {
        if (!_lastResult.IsValid) return false;
        if (_lastResult.ActiveTier is LicenseTier.Enterprise or LicenseTier.OEMSovereign) return true;

        return featureName.ToLowerInvariant() switch
        {
            "aisuite" => _currentPayload?.EnableAiSuite ?? false,
            "bursting" => _currentPayload?.EnableReportBursting ?? false,
            "truevectorredaction" => _currentPayload?.EnableTrueVectorRedaction ?? false,
            _ => true
        };
    }
}
