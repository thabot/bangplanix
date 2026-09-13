namespace Bangplanix.Core.Distributed;

/// <summary>
/// Abstraction for multi-pod distributed cloud storage synchronization (S3, Azure Blob, MinIO)
/// for report templates, custom font assets, and shared image resources.
/// </summary>
public interface IDistributedStorageSyncProvider
{
    /// <summary>
    /// Retrieves file bytes from cloud storage, utilizing high-performance in-memory cache and ETag checks.
    /// </summary>
    Task<byte[]> GetFileAsync(string category, string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Uploads or updates a file in distributed cloud storage and broadcasts cache invalidation.
    /// </summary>
    Task PutFileAsync(string category, string key, byte[] content, string? contentType = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists all file keys present under a category (e.g., "fonts", "templates", "logos").
    /// </summary>
    Task<IReadOnlyList<string>> ListFilesAsync(string category, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a file from storage and removes it from local caches.
    /// </summary>
    Task DeleteFileAsync(string category, string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Clears the local memory cache to force fresh fetches from cloud storage.
    /// </summary>
    void InvalidateCache(string? category = null, string? key = null);
}
