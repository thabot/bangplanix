using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace Bangplanix.Core.Bursting;

/// <summary>
/// Policy defining dynamic per-recipient password encryption rules for confidential bursting.
/// </summary>
public sealed class RecipientSecurityPolicy
{
    public bool IsEnabled { get; set; } = true;
    public string PasswordPattern { get; set; } = "{NationalId:Last4}";
    public string? FallbackPassword { get; set; } = "Bangplanix@2026";
    public bool EnforceAes256Encryption { get; set; } = true;
}

/// <summary>
/// Engine for evaluating dynamic recipient passwords and encrypting confidential report byte streams.
/// </summary>
public sealed class RecipientPasswordEncryptionEngine
{
    /// <summary>
    /// Evaluates the password for a recipient based on their metadata dictionary.
    /// </summary>
    public static string GenerateRecipientPassword(RecipientSecurityPolicy policy, IDictionary<string, object?> recipientMetadata)
    {
        if (policy == null || !policy.IsEnabled) return string.Empty;

        string pattern = policy.PasswordPattern;
        string result = pattern;

        foreach (var (k, v) in recipientMetadata)
        {
            string valStr = v?.ToString() ?? string.Empty;

            // Check for Last4 modifier e.g. {NationalId:Last4}
            if (pattern.Contains($"{{{k}:Last4}}"))
            {
                string last4 = valStr.Length >= 4 ? valStr.Substring(valStr.Length - 4) : valStr;
                result = result.Replace($"{{{k}:Last4}}", last4);
            }

            // Standard replacement e.g. {EmployeeId}
            result = result.Replace($"{{{k}}}", valStr);
        }

        if (string.IsNullOrWhiteSpace(result) || result.Contains('{'))
        {
            return policy.FallbackPassword ?? "Bangplanix@2026";
        }

        return result;
    }

    /// <summary>
    /// Encrypts document bytes using AES-256 with the generated recipient password.
    /// </summary>
    public static byte[] EncryptDocumentBytes(byte[] rawBytes, string password)
    {
        if (rawBytes == null || rawBytes.Length == 0) return Array.Empty<byte>();
        if (string.IsNullOrEmpty(password)) return rawBytes;

        // Derive 256-bit key from password using PBKDF2
        byte[] salt = new byte[16];
        RandomNumberGenerator.Fill(salt);

        using var kdf = new Rfc2898DeriveBytes(password, salt, 10_000, HashAlgorithmName.SHA256);
        byte[] key = kdf.GetBytes(32); // 256-bit
        byte[] iv = kdf.GetBytes(16);  // 128-bit

        using var aes = Aes.Create();
        aes.Key = key;
        aes.IV = iv;

        using var ms = new MemoryStream();
        // Prepend salt to output stream
        ms.Write(salt, 0, salt.Length);

        using (var cs = new CryptoStream(ms, aes.CreateEncryptor(), CryptoStreamMode.Write))
        {
            cs.Write(rawBytes, 0, rawBytes.Length);
            cs.FlushFinalBlock();
        }

        return ms.ToArray();
    }
}
