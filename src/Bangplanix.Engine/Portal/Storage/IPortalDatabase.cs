namespace Bangplanix.Engine.Portal.Storage;

public sealed record PortalUser(string Username, string PasswordHash, string Role, DateTime CreatedAtUtc);
public sealed record PortalSession(string Token, string Username, string Role, DateTime ExpiresAtUtc);
public sealed record PortalAuditLog(long Id, string Action, string Username, string Details, DateTime TimestampUtc);

public interface IPortalDatabase : IAsyncDisposable
{
    Task InitializeAsync(CancellationToken cancellationToken = default);
    Task<PortalUser?> GetUserAsync(string username, CancellationToken cancellationToken = default);
    Task CreateUserAsync(string username, string passwordHash, string role = "admin", CancellationToken cancellationToken = default);
    Task<bool> ValidatePasswordAsync(string username, string password, CancellationToken cancellationToken = default);
    Task<PortalSession> CreateSessionAsync(string username, string role, TimeSpan? ttl = null, CancellationToken cancellationToken = default);
    Task<PortalSession?> ValidateSessionAsync(string token, CancellationToken cancellationToken = default);
    Task RevokeSessionAsync(string token, CancellationToken cancellationToken = default);
    Task LogAuditAsync(string action, string username, string details, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PortalAuditLog>> GetAuditLogsAsync(int limit = 50, CancellationToken cancellationToken = default);
}
