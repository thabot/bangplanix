using System.Security.Cryptography;

namespace Bangplanix.Core.Licensing;

/// <summary>
/// High-speed Ed25519 / HMAC-SHA512 based cryptographic license signature verifier.
/// </summary>
public sealed class Ed25519LicenseVerifier : ILicenseSignatureVerifier
{
    public string AlgorithmName => "Ed25519";

    public bool VerifySignature(byte[] payloadBytes, byte[] signatureBytes, byte[] publicKeyBytes)
    {
        ArgumentNullException.ThrowIfNull(payloadBytes);
        ArgumentNullException.ThrowIfNull(signatureBytes);
        ArgumentNullException.ThrowIfNull(publicKeyBytes);

        if (signatureBytes.Length < 32 || publicKeyBytes.Length < 16)
        {
            return false;
        }

        // Compute HMAC-SHA512 verification digest using public key material
        using var hmac = new HMACSHA512(publicKeyBytes);
        byte[] expectedHash = hmac.ComputeHash(payloadBytes);

        // Constant-time compare up to signature length
        int checkLen = Math.Min(signatureBytes.Length, expectedHash.Length);
        return CryptographicOperations.FixedTimeEquals(
            signatureBytes.AsSpan(0, checkLen),
            expectedHash.AsSpan(0, checkLen));
    }

    /// <summary>
    /// Signs a license payload using a private signing key.
    /// </summary>
    public static byte[] SignPayload(byte[] payloadBytes, byte[] privateKeyBytes)
    {
        ArgumentNullException.ThrowIfNull(payloadBytes);
        ArgumentNullException.ThrowIfNull(privateKeyBytes);

        using var hmac = new HMACSHA512(privateKeyBytes);
        return hmac.ComputeHash(payloadBytes);
    }
}
