using System.IO.Compression;
using System.Text;
using System.Text.Json;

namespace Bangplanix.Core.Bursting;

/// <summary>
/// Manifest item descriptor inside a consolidated Zip bundle.
/// </summary>
public sealed record BundleManifestItem(string SliceKey, string FileName, int FileSizeBytes, DateTime GeneratedAtUtc);

/// <summary>
/// Manifest cover sheet for a group zip bundle.
/// </summary>
public sealed class BundleManifest
{
    public string GroupKey { get; set; } = string.Empty;
    public DateTime BundleCreatedUtc { get; set; } = DateTime.UtcNow;
    public int TotalDocuments => Items.Count;
    public List<BundleManifestItem> Items { get; set; } = new();
}

/// <summary>
/// Multi-Recipient Zip Archive Bundler consolidating documents into a single departmental zip package.
/// </summary>
public sealed class DepartmentZipBundler
{
    /// <summary>
    /// Consolidates multiple slice documents into a compressed Zip archive with a manifest cover sheet.
    /// </summary>
    public static byte[] CreateZipBundle(
        string groupKey,
        IEnumerable<(string SliceKey, string FileName, byte[] DocumentBytes)> documentSlices)
    {
        var manifest = new BundleManifest { GroupKey = groupKey };

        using var ms = new MemoryStream();
        using (var archive = new ZipArchive(ms, ZipArchiveMode.Create, true, Encoding.UTF8))
        {
            foreach (var (sliceKey, fileName, docBytes) in documentSlices)
            {
                if (docBytes == null || docBytes.Length == 0) continue;

                var entry = archive.CreateEntry(fileName, CompressionLevel.Optimal);
                using (var entryStream = entry.Open())
                {
                    entryStream.Write(docBytes, 0, docBytes.Length);
                }

                manifest.Items.Add(new BundleManifestItem(sliceKey, fileName, docBytes.Length, DateTime.UtcNow));
            }

            // Write Manifest JSON file inside the archive
            var manifestEntry = archive.CreateEntry("bundle_manifest.json", CompressionLevel.Fastest);
            using (var manifestStream = manifestEntry.Open())
            {
                byte[] manifestBytes = JsonSerializer.SerializeToUtf8Bytes(manifest, new JsonSerializerOptions { WriteIndented = true });
                manifestStream.Write(manifestBytes, 0, manifestBytes.Length);
            }
        }

        return ms.ToArray();
    }
}
