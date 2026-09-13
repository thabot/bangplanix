using System.Security.Cryptography;
using System.Text;

namespace Bangplanix.Core.Security;

public static class AesGcmCrypto
{
    private const int NonceSizeBytes = 12; // 96 bits for GCM
    private const int TagSizeBytes = 16;   // 128 bits for GCM Tag
    private const int KeySizeBytes = 32;   // 256 bits for AES-256

    public static string Encrypt(string plainText, string? masterKey = null, string? associatedData = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(plainText);

        var key = ResolveKey(masterKey);
        var plainBytes = Encoding.UTF8.GetBytes(plainText);
        var aadBytes = string.IsNullOrEmpty(associatedData) ? null : Encoding.UTF8.GetBytes(associatedData);

        var nonce = new byte[NonceSizeBytes];
        RandomNumberGenerator.Fill(nonce);

        var cipherBytes = new byte[plainBytes.Length];
        var tag = new byte[TagSizeBytes];

        using var aesGcm = new AesGcm(key, TagSizeBytes);
        aesGcm.Encrypt(nonce, plainBytes, cipherBytes, tag, aadBytes);

        // Format: Nonce (12) + Tag (16) + Ciphertext
        var combined = new byte[NonceSizeBytes + TagSizeBytes + cipherBytes.Length];
        Buffer.BlockCopy(nonce, 0, combined, 0, NonceSizeBytes);
        Buffer.BlockCopy(tag, 0, combined, NonceSizeBytes, TagSizeBytes);
        Buffer.BlockCopy(cipherBytes, 0, combined, NonceSizeBytes + TagSizeBytes, cipherBytes.Length);

        return "enc:" + Convert.ToBase64String(combined);
    }

    public static string Decrypt(string encryptedText, string? masterKey = null, string? associatedData = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(encryptedText);

        if (!encryptedText.StartsWith("enc:", StringComparison.OrdinalIgnoreCase))
        {
            // If not encrypted, return as is (plaintext fallback)
            return encryptedText;
        }

        var key = ResolveKey(masterKey);
        var aadBytes = string.IsNullOrEmpty(associatedData) ? null : Encoding.UTF8.GetBytes(associatedData);
        var base64 = encryptedText[4..];
        var combined = Convert.FromBase64String(base64);

        if (combined.Length < NonceSizeBytes + TagSizeBytes)
        {
            throw new CryptographicException("Invalid ciphertext payload: insufficient data length.");
        }

        var nonce = new byte[NonceSizeBytes];
        var tag = new byte[TagSizeBytes];
        var cipherLen = combined.Length - NonceSizeBytes - TagSizeBytes;
        var cipherBytes = new byte[cipherLen];

        Buffer.BlockCopy(combined, 0, nonce, 0, NonceSizeBytes);
        Buffer.BlockCopy(combined, NonceSizeBytes, tag, 0, TagSizeBytes);
        Buffer.BlockCopy(combined, NonceSizeBytes + TagSizeBytes, cipherBytes, 0, cipherLen);

        var plainBytes = new byte[cipherLen];

        using var aesGcm = new AesGcm(key, TagSizeBytes);
        aesGcm.Decrypt(nonce, cipherBytes, tag, plainBytes, aadBytes);

        return Encoding.UTF8.GetString(plainBytes);
    }

    private static byte[] ResolveKey(string? masterKey)
    {
        var rawKey = masterKey;
        if (string.IsNullOrWhiteSpace(rawKey))
        {
            rawKey = Environment.GetEnvironmentVariable("BANGPLANIX_MASTER_KEY")
                  ?? Environment.GetEnvironmentVariable("THABOT_MASTER_KEY")
                  ?? "Bangplanix_Default_Dev_Master_Key_2026_Sec!";
        }

        // Derive fixed 256-bit key via SHA-256
        return SHA256.HashData(Encoding.UTF8.GetBytes(rawKey));
    }
}
