using System.Security.Cryptography;
using System.Text;

namespace Bangplanix.Core.Plugins;

public class PluginSignatureInfo
{
    public required string PublisherId { get; init; }
    public required string Algorithm { get; init; } // e.g., "HMAC-SHA256", "RSA-SHA256"
    public required string SignatureHex { get; init; }
    public required string AssemblyHashSha256 { get; init; }
    public DateTimeOffset SignedAt { get; init; } = DateTimeOffset.UtcNow;
}

public static class PluginSignatureValidator
{
    public static string ComputeAssemblyHash(byte[] assemblyBytes)
    {
        var hash = SHA256.HashData(assemblyBytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    public static PluginSignatureInfo CreateSignature(byte[] assemblyBytes, string publisherId, byte[] secretSigningKey)
    {
        var hashHex = ComputeAssemblyHash(assemblyBytes);
        var signData = $"{publisherId}|{hashHex}";
        var signatureBytes = HMACSHA256.HashData(secretSigningKey, Encoding.UTF8.GetBytes(signData));

        return new PluginSignatureInfo
        {
            PublisherId = publisherId,
            Algorithm = "HMAC-SHA256",
            AssemblyHashSha256 = hashHex,
            SignatureHex = Convert.ToHexString(signatureBytes).ToLowerInvariant(),
            SignedAt = DateTimeOffset.UtcNow
        };
    }

    public static bool VerifySignature(byte[] assemblyBytes, PluginSignatureInfo signatureInfo, byte[] secretVerificationKey)
    {
        if (signatureInfo == null) return false;

        var computedHash = ComputeAssemblyHash(assemblyBytes);
        if (!string.Equals(computedHash, signatureInfo.AssemblyHashSha256, StringComparison.OrdinalIgnoreCase))
        {
            return false; // Assembly has been altered/tampered
        }

        var signData = $"{signatureInfo.PublisherId}|{signatureInfo.AssemblyHashSha256}";
        var expectedSignature = HMACSHA256.HashData(secretVerificationKey, Encoding.UTF8.GetBytes(signData));
        var expectedHex = Convert.ToHexString(expectedSignature).ToLowerInvariant();

        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(expectedHex),
            Encoding.UTF8.GetBytes(signatureInfo.SignatureHex));
    }
}
