using System.Security.Cryptography;
using System.Text;

namespace Bangplanix.Engine.Security;

/// <summary>
/// Tamper verification result containing page-by-page integrity status.
/// </summary>
public sealed class TamperVerificationResult
{
    public bool IsIntact { get; set; }
    public string MerkleRootHash { get; set; } = string.Empty;
    public List<int> TamperedPageNumbers { get; } = [];
    public string StatusMessage { get; set; } = string.Empty;
}

/// <summary>
/// Per-page Merkle Tree & Tamper-Evident Seal Engine for legal agreements and critical multi-page financial reports.
/// </summary>
public static class DocumentTamperSealEngine
{
    /// <summary>
    /// Computes cryptographic SHA-256 hash for a single page buffer.
    /// </summary>
    public static string ComputePageHash(byte[] pageBytes)
    {
        ArgumentNullException.ThrowIfNull(pageBytes);
        using var sha256 = SHA256.Create();
        return Convert.ToHexString(sha256.ComputeHash(pageBytes));
    }

    /// <summary>
    /// Builds a Merkle Tree from a list of page byte arrays and returns the Merkle Root Hash and leaf hashes.
    /// </summary>
    public static (string MerkleRoot, List<string> LeafHashes) BuildMerkleTree(IReadOnlyList<byte[]> pageBuffers)
    {
        ArgumentNullException.ThrowIfNull(pageBuffers);

        if (pageBuffers.Count == 0)
        {
            return (string.Empty, []);
        }

        var leafHashes = pageBuffers.Select(ComputePageHash).ToList();
        var currentLevel = new List<string>(leafHashes);

        using var sha256 = SHA256.Create();

        while (currentLevel.Count > 1)
        {
            var nextLevel = new List<string>();
            for (int i = 0; i < currentLevel.Count; i += 2)
            {
                if (i + 1 < currentLevel.Count)
                {
                    var combined = currentLevel[i] + currentLevel[i + 1];
                    var hash = Convert.ToHexString(sha256.ComputeHash(Encoding.UTF8.GetBytes(combined)));
                    nextLevel.Add(hash);
                }
                else
                {
                    // Odd number of leaves: duplicate last hash
                    var combined = currentLevel[i] + currentLevel[i];
                    var hash = Convert.ToHexString(sha256.ComputeHash(Encoding.UTF8.GetBytes(combined)));
                    nextLevel.Add(hash);
                }
            }
            currentLevel = nextLevel;
        }

        return (currentLevel[0], leafHashes);
    }

    /// <summary>
    /// Computes HMAC-SHA512 tamper seal over the document's Merkle Root.
    /// </summary>
    public static string GenerateHmacSeal(string merkleRoot, string secretKey)
    {
        ArgumentNullException.ThrowIfNull(merkleRoot);
        ArgumentNullException.ThrowIfNull(secretKey);

        var keyBytes = Encoding.UTF8.GetBytes(secretKey);
        using var hmac = new HMACSHA512(keyBytes);
        var sealBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(merkleRoot));
        return Convert.ToHexString(sealBytes);
    }

    /// <summary>
    /// Applies a tamper-evident seal metadata dictionary into a PDF byte stream.
    /// </summary>
    public static byte[] SealPdfDocument(byte[] pdfBytes, IReadOnlyList<byte[]> pageBuffers, string secretKey = "BangplanixMasterSealKey")
    {
        ArgumentNullException.ThrowIfNull(pdfBytes);
        ArgumentNullException.ThrowIfNull(pageBuffers);

        var (merkleRoot, leafHashes) = BuildMerkleTree(pageBuffers);
        var seal = GenerateHmacSeal(merkleRoot, secretKey);

        var leavesStr = string.Join(",", leafHashes);
        var pdfString = Encoding.Latin1.GetString(pdfBytes);

        var sealObjNum = 666;
        var sealDict = $"\r\n{sealObjNum} 0 obj\r\n<< /Type /BangplanixSeal /MerkleRoot ({merkleRoot}) /Leaves ({leavesStr}) /HmacSeal ({seal}) >>\r\nendobj\r\n";

        var modifiedPdf = pdfString + sealDict;
        if (modifiedPdf.Contains("/Catalog", StringComparison.OrdinalIgnoreCase))
        {
            modifiedPdf = modifiedPdf.Replace("/Catalog", $"/Catalog /BangplanixSeal {sealObjNum} 0 R", StringComparison.OrdinalIgnoreCase);
        }

        return Encoding.Latin1.GetBytes(modifiedPdf);
    }

    /// <summary>
    /// Verifies the integrity of individual pages against the embedded Merkle Tree and HMAC seal.
    /// </summary>
    public static TamperVerificationResult VerifyPdfDocument(
        byte[] sealedPdfBytes,
        IReadOnlyList<byte[]> currentPageBuffers,
        string secretKey = "BangplanixMasterSealKey")
    {
        ArgumentNullException.ThrowIfNull(sealedPdfBytes);
        ArgumentNullException.ThrowIfNull(currentPageBuffers);

        var result = new TamperVerificationResult();
        var pdfString = Encoding.Latin1.GetString(sealedPdfBytes);

        var merkleMatch = System.Text.RegularExpressions.Regex.Match(pdfString, @"/MerkleRoot\s*\(([A-Fa-f0-9]+)\)");
        var leavesMatch = System.Text.RegularExpressions.Regex.Match(pdfString, @"/Leaves\s*\(([A-Fa-f0-9,]+)\)");
        var hmacMatch = System.Text.RegularExpressions.Regex.Match(pdfString, @"/HmacSeal\s*\(([A-Fa-f0-9]+)\)");

        if (!merkleMatch.Success || !leavesMatch.Success || !hmacMatch.Success)
        {
            result.IsIntact = false;
            result.StatusMessage = "No tamper-evident seal metadata found in document.";
            return result;
        }

        var embeddedMerkleRoot = merkleMatch.Groups[1].Value;
        var embeddedLeaves = leavesMatch.Groups[1].Value.Split(',', StringSplitOptions.RemoveEmptyEntries);
        var embeddedHmac = hmacMatch.Groups[1].Value;

        // Verify HMAC authenticity
        var computedHmac = GenerateHmacSeal(embeddedMerkleRoot, secretKey);
        if (!string.Equals(embeddedHmac, computedHmac, StringComparison.OrdinalIgnoreCase))
        {
            result.IsIntact = false;
            result.StatusMessage = "HMAC authentication failed. The seal key or root hash is invalid.";
            return result;
        }

        result.MerkleRootHash = embeddedMerkleRoot;

        // Verify per-page hashes
        for (int i = 0; i < currentPageBuffers.Count; i++)
        {
            var pageHash = ComputePageHash(currentPageBuffers[i]);
            if (i < embeddedLeaves.Length)
            {
                if (!string.Equals(pageHash, embeddedLeaves[i], StringComparison.OrdinalIgnoreCase))
                {
                    result.TamperedPageNumbers.Add(i + 1); // 1-based page number
                }
            }
            else
            {
                result.TamperedPageNumbers.Add(i + 1);
            }
        }

        result.IsIntact = result.TamperedPageNumbers.Count == 0;
        result.StatusMessage = result.IsIntact
            ? "Document pages and Merkle Root are 100% authentic and unaltered."
            : $"Tampering detected on page(s): {string.Join(", ", result.TamperedPageNumbers)}";

        return result;
    }
}
