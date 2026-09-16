using System.Security.Cryptography;
using System.Text;
using Npgsql;

namespace Bangplanix.Engine.Portal.Storage;

public sealed class PostgreSqlPortalDatabase : IPortalDatabase
{
    private readonly string _connectionString;
    private NpgsqlDataSource? _dataSource;

    public PostgreSqlPortalDatabase(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        var builder = new NpgsqlDataSourceBuilder(_connectionString);
        _dataSource = builder.Build();

        string ddl = """
            CREATE TABLE IF NOT EXISTS portal_users (
                username VARCHAR(100) PRIMARY KEY,
                password_hash TEXT NOT NULL,
                role VARCHAR(50) NOT NULL DEFAULT 'admin',
                created_at_utc TIMESTAMPTZ NOT NULL
            );

            CREATE TABLE IF NOT EXISTS portal_sessions (
                token VARCHAR(128) PRIMARY KEY,
                username VARCHAR(100) NOT NULL,
                role VARCHAR(50) NOT NULL,
                expires_at_utc TIMESTAMPTZ NOT NULL
            );

            CREATE TABLE IF NOT EXISTS portal_audit_logs (
                id BIGSERIAL PRIMARY KEY,
                action VARCHAR(100) NOT NULL,
                username VARCHAR(100) NOT NULL,
                details TEXT NOT NULL,
                timestamp_utc TIMESTAMPTZ NOT NULL
            );
        """;

        await using var cmd = _dataSource.CreateCommand(ddl);
        await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);

        string defaultUser = Environment.GetEnvironmentVariable("BANGPLANIX_PORTAL_USER") ?? "admin";
        string defaultPass = Environment.GetEnvironmentVariable("BANGPLANIX_PORTAL_PASSWORD") 
                             ?? Environment.GetEnvironmentVariable("THABOT_MASTER_KEY") 
                             ?? "bangplanix2026!";

        var existing = await GetUserAsync(defaultUser, cancellationToken).ConfigureAwait(false);
        if (existing == null)
        {
            await CreateUserAsync(defaultUser, SqlitePortalDatabase.HashPassword(defaultPass), "admin", cancellationToken).ConfigureAwait(false);
            await LogAuditAsync("SEED_DEFAULT_ADMIN", "system", $"Default admin '{defaultUser}' created", cancellationToken).ConfigureAwait(false);
        }
    }

    public async Task<PortalUser?> GetUserAsync(string username, CancellationToken cancellationToken = default)
    {
        if (_dataSource == null) await InitializeAsync(cancellationToken).ConfigureAwait(false);

        await using var cmd = _dataSource!.CreateCommand("SELECT username, password_hash, role, created_at_utc FROM portal_users WHERE username = $1 LIMIT 1");
        cmd.Parameters.AddWithValue(username);

        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        if (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            return new PortalUser(
                reader.GetString(0),
                reader.GetString(1),
                reader.GetString(2),
                reader.GetDateTime(3).ToUniversalTime()
            );
        }
        return null;
    }

    public async Task CreateUserAsync(string username, string passwordHash, string role = "admin", CancellationToken cancellationToken = default)
    {
        if (_dataSource == null) await InitializeAsync(cancellationToken).ConfigureAwait(false);

        await using var cmd = _dataSource!.CreateCommand("INSERT INTO portal_users (username, password_hash, role, created_at_utc) VALUES ($1, $2, $3, $4) ON CONFLICT (username) DO UPDATE SET password_hash = EXCLUDED.password_hash, role = EXCLUDED.role");
        cmd.Parameters.AddWithValue(username);
        cmd.Parameters.AddWithValue(passwordHash);
        cmd.Parameters.AddWithValue(role);
        cmd.Parameters.AddWithValue(DateTime.UtcNow);

        await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<bool> ValidatePasswordAsync(string username, string password, CancellationToken cancellationToken = default)
    {
        var user = await GetUserAsync(username, cancellationToken).ConfigureAwait(false);
        if (user == null) return false;
        return SqlitePortalDatabase.VerifyPassword(password, user.PasswordHash);
    }

    public async Task<PortalSession> CreateSessionAsync(string username, string role, TimeSpan? ttl = null, CancellationToken cancellationToken = default)
    {
        if (_dataSource == null) await InitializeAsync(cancellationToken).ConfigureAwait(false);

        string token = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        var expiresAt = DateTime.UtcNow.Add(ttl ?? TimeSpan.FromHours(12));

        await using var cmd = _dataSource!.CreateCommand("INSERT INTO portal_sessions (token, username, role, expires_at_utc) VALUES ($1, $2, $3, $4)");
        cmd.Parameters.AddWithValue(token);
        cmd.Parameters.AddWithValue(username);
        cmd.Parameters.AddWithValue(role);
        cmd.Parameters.AddWithValue(expiresAt);

        await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        return new PortalSession(token, username, role, expiresAt);
    }

    public async Task<PortalSession?> ValidateSessionAsync(string token, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(token)) return null;
        if (_dataSource == null) await InitializeAsync(cancellationToken).ConfigureAwait(false);

        await using var cmd = _dataSource!.CreateCommand("SELECT token, username, role, expires_at_utc FROM portal_sessions WHERE token = $1 LIMIT 1");
        cmd.Parameters.AddWithValue(token);

        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        if (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            var expires = reader.GetDateTime(3).ToUniversalTime();
            if (expires > DateTime.UtcNow)
            {
                return new PortalSession(reader.GetString(0), reader.GetString(1), reader.GetString(2), expires);
            }
            await RevokeSessionAsync(token, cancellationToken).ConfigureAwait(false);
        }
        return null;
    }

    public async Task RevokeSessionAsync(string token, CancellationToken cancellationToken = default)
    {
        if (_dataSource == null) return;
        await using var cmd = _dataSource.CreateCommand("DELETE FROM portal_sessions WHERE token = $1");
        cmd.Parameters.AddWithValue(token);
        await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task LogAuditAsync(string action, string username, string details, CancellationToken cancellationToken = default)
    {
        if (_dataSource == null) await InitializeAsync(cancellationToken).ConfigureAwait(false);

        await using var cmd = _dataSource!.CreateCommand("INSERT INTO portal_audit_logs (action, username, details, timestamp_utc) VALUES ($1, $2, $3, $4)");
        cmd.Parameters.AddWithValue(action);
        cmd.Parameters.AddWithValue(username);
        cmd.Parameters.AddWithValue(details);
        cmd.Parameters.AddWithValue(DateTime.UtcNow);

        await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<PortalAuditLog>> GetAuditLogsAsync(int limit = 50, CancellationToken cancellationToken = default)
    {
        if (_dataSource == null) await InitializeAsync(cancellationToken).ConfigureAwait(false);

        var list = new List<PortalAuditLog>();
        await using var cmd = _dataSource!.CreateCommand("SELECT id, action, username, details, timestamp_utc FROM portal_audit_logs ORDER BY id DESC LIMIT $1");
        cmd.Parameters.AddWithValue(limit);

        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            list.Add(new PortalAuditLog(
                reader.GetInt64(0),
                reader.GetString(1),
                reader.GetString(2),
                reader.GetString(3),
                reader.GetDateTime(4).ToUniversalTime()
            ));
        }
        return list;
    }

    public async ValueTask DisposeAsync()
    {
        if (_dataSource != null)
        {
            await _dataSource.DisposeAsync().ConfigureAwait(false);
            _dataSource = null;
        }
    }
}
