using System.Collections.Concurrent;
using System.Security.Cryptography;
using Bangplanix.Core.Distributed;

namespace Bangplanix.Engine.Distributed;

/// <summary>
/// Replication record tracking status of a file across multi-region cloud buckets.
/// </summary>
public sealed class ReplicationRecord
{
    public string Category { get; set; } = string.Empty;
    public string Key { get; set; } = string.Empty;
    public string PrimarySha256 { get; set; } = string.Empty;
    public string SecondarySha256 { get; set; } = string.Empty;
    public bool IsSynchronized { get; set; }
    public DateTime LastReplicatedAtUtc { get; set; } = DateTime.UtcNow;
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Active-Active / Disaster-Recovery Cross-Region Cloud Storage Replicator.
/// Replicates templates, fonts, and generated reports across multi-region object stores with cryptographic integrity validation.
/// </summary>
public sealed class CrossRegionStorageReplicator
{
    private readonly IDistributedStorageSyncProvider _primaryStore;
    private readonly IDistributedStorageSyncProvider _secondaryStore;
    private readonly ConcurrentDictionary<string, ReplicationRecord> _replicationAudit = new(StringComparer.OrdinalIgnoreCase);

    public CrossRegionStorageReplicator(
        IDistributedStorageSyncProvider primaryStore,
        IDistributedStorageSyncProvider secondaryStore)
    {
        _primaryStore = primaryStore ?? throw new ArgumentNullException(nameof(primaryStore));
        _secondaryStore = secondaryStore ?? throw new ArgumentNullException(nameof(secondaryStore));
    }

    /// <summary>
    /// Synchronizes a specific file from primary to secondary storage with SHA-256 verification.
    /// </summary>
    public async Task<ReplicationRecord> ReplicateFileAsync(
        string category,
        string key,
        CancellationToken cancellationToken = default)
    {
        string recordKey = $"{category}/{key}";
        var record = new ReplicationRecord { Category = category, Key = key };

        try
        {
            byte[] primaryBytes = await _primaryStore.GetFileAsync(category, key, cancellationToken).ConfigureAwait(false);
            byte[] primaryHash = SHA256.HashData(primaryBytes);
            record.PrimarySha256 = Convert.ToHexString(primaryHash);

            // Put into secondary
            await _secondaryStore.PutFileAsync(category, key, primaryBytes, null, cancellationToken).ConfigureAwait(false);

            // Verify secondary read-back
            byte[] secondaryBytes = await _secondaryStore.GetFileAsync(category, key, cancellationToken).ConfigureAwait(false);
            byte[] secondaryHash = SHA256.HashData(secondaryBytes);
            record.SecondarySha256 = Convert.ToHexString(secondaryHash);

            if (CryptographicOperations.FixedTimeEquals(primaryHash, secondaryHash))
            {
                record.IsSynchronized = true;
                record.LastReplicatedAtUtc = DateTime.UtcNow;
            }
            else
            {
                record.IsSynchronized = false;
                record.ErrorMessage = "Integrity mismatch: Secondary checksum differs from primary.";
            }
        }
        catch (Exception ex)
        {
            record.IsSynchronized = false;
            record.ErrorMessage = ex.Message;
        }

        _replicationAudit[recordKey] = record;
        return record;
    }

    /// <summary>
    /// Synchronizes all files under a category between regions.
    /// </summary>
    public async Task<IReadOnlyList<ReplicationRecord>> ReplicateCategoryAsync(
        string category,
        CancellationToken cancellationToken = default)
    {
        var files = await _primaryStore.ListFilesAsync(category, cancellationToken).ConfigureAwait(false);
        var results = new List<ReplicationRecord>();

        foreach (var file in files)
        {
            var res = await ReplicateFileAsync(category, file, cancellationToken).ConfigureAwait(false);
            results.Add(res);
        }

        return results;
    }

    /// <summary>
    /// Gets the replication audit status for a file.
    /// </summary>
    public ReplicationRecord? GetReplicationStatus(string category, string key)
    {
        string recordKey = $"{category}/{key}";
        _replicationAudit.TryGetValue(recordKey, out var record);
        return record;
    }
}
