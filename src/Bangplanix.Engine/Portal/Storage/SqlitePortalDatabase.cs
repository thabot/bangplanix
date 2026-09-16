using System.Security.Cryptography;
using System.Text;
using Microsoft.Data.Sqlite;

namespace Bangplanix.Engine.Portal.Storage;

public sealed class SqlitePortalDatabase : IPortalDatabase
{
    private readonly string _connectionString;
    private SqliteConnection? _connection;

    public SqlitePortalDatabase(string connectionString = "Data Source=/app/volumes/data/portal.db")
    {
        _connectionString = connectionString;
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        // Ensure directory exists if it's a file path
        if (_connectionString.Contains("Data Source=", StringComparison.OrdinalIgnoreCase))
        {
            var parts = _connectionString.Split(';');
            var ds = parts.FirstOrDefault(p => p.Trim().StartsWith("Data Source=", StringComparison.OrdinalIgnoreCase));
            if (ds != null)
            {
                var filePath = ds.Split('=')[1].Trim();
                if (!string.IsNullOrWhiteSpace(filePath) && filePath != ":memory:")
                {
                    var dir = Path.GetDirectoryName(Path.GetFullPath(filePath));
                    if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                    {
                        Directory.CreateDirectory(dir);
                    }
                }
            }
        }

        _connection = new SqliteConnection(_connectionString);
        await _connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        string ddl = """
            CREATE TABLE IF NOT EXISTS portal_users (
                username TEXT PRIMARY KEY,
                password_hash TEXT NOT NULL,
                role TEXT NOT NULL DEFAULT 'admin',
                created_at_utc TEXT NOT NULL
            );

            CREATE TABLE IF NOT EXISTS portal_sessions (
                token TEXT PRIMARY KEY,
                username TEXT NOT NULL,
                role TEXT NOT NULL,
                expires_at_utc TEXT NOT NULL
            );

            CREATE TABLE IF NOT EXISTS portal_audit_logs (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                action TEXT NOT NULL,
                username TEXT NOT NULL,
                details TEXT NOT NULL,
                timestamp_utc TEXT NOT NULL
            );
        """;

        using var cmd = _connection.CreateCommand();
        cmd.CommandText = ddl;
        await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);

        // Seed default admin if no user exists
        string defaultUser = Environment.GetEnvironmentVariable("BANGPLANIX_PORTAL_USER") ?? "admin";
        string defaultPass = Environment.GetEnvironmentVariable("BANGPLANIX_PORTAL_PASSWORD") 
                             ?? Environment.GetEnvironmentVariable("THABOT_MASTER_KEY") 
                             ?? "bangplanix2026!";

        var existing = await GetUserAsync(defaultUser, cancellationToken).ConfigureAwait(false);
        if (existing == null)
        {
            await CreateUserAsync(defaultUser, HashPassword(defaultPass), "admin", cancellationToken).ConfigureAwait(false);
            await LogAuditAsync("SEED_DEFAULT_ADMIN", "system", $"Default admin '{defaultUser}' created", cancellationToken).ConfigureAwait(false);
        }
    }

    public async Task<PortalUser?> GetUserAsync(string username, CancellationToken cancellationToken = default)
    {
        if (_connection == null) await InitializeAsync(cancellationToken).ConfigureAwait(false);

        using var cmd = _connection!.CreateCommand();
        cmd.CommandText = "SELECT username, password_hash, role, created_at_utc FROM portal_users WHERE username = @u LIMIT 1";
        cmd.Parameters.AddWithValue("@u", username);

        using var reader = await cmd.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        if (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            return new PortalUser(
                reader.GetString(0),
                reader.GetString(1),
                reader.GetString(2),
                DateTime.Parse(reader.GetString(3))
            );
        }
        return null;
    }

    public async Task CreateUserAsync(string username, string passwordHash, string role = "admin", CancellationToken cancellationToken = default)
    {
        if (_connection == null) await InitializeAsync(cancellationToken).ConfigureAwait(false);

        using var cmd = _connection!.CreateCommand();
        cmd.CommandText = "INSERT OR REPLACE INTO portal_users (username, password_hash, role, created_at_utc) VALUES (@u, @p, @r, @c)";
        cmd.Parameters.AddWithValue("@u", username);
        cmd.Parameters.AddWithValue("@p", passwordHash);
        cmd.Parameters.AddWithValue("@r", role);
        cmd.Parameters.AddWithValue("@c", DateTime.UtcNow.ToString("O"));

        await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<bool> ValidatePasswordAsync(string username, string password, CancellationToken cancellationToken = default)
    {
        var user = await GetUserAsync(username, cancellationToken).ConfigureAwait(false);
        if (user == null) return false;
        return VerifyPassword(password, user.PasswordHash);
    }

    public async Task<PortalSession> CreateSessionAsync(string username, string role, TimeSpan? ttl = null, CancellationToken cancellationToken = default)
    {
        if (_connection == null) await InitializeAsync(cancellationToken).ConfigureAwait(false);

        string token = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        var expiresAt = DateTime.UtcNow.Add(ttl ?? TimeSpan.FromHours(12));

        using var cmd = _connection!.CreateCommand();
        cmd.CommandText = "INSERT INTO portal_sessions (token, username, role, expires_at_utc) VALUES (@t, @u, @r, @e)";
        cmd.Parameters.AddWithValue("@t", token);
        cmd.Parameters.AddWithValue("@u", username);
        cmd.Parameters.AddWithValue("@r", role);
        cmd.Parameters.AddWithValue("@e", expiresAt.ToString("O"));

        await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        return new PortalSession(token, username, role, expiresAt);
    }

    public async Task<PortalSession?> ValidateSessionAsync(string token, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(token)) return null;
        if (_connection == null) await InitializeAsync(cancellationToken).ConfigureAwait(false);

        using var cmd = _connection!.CreateCommand();
        cmd.CommandText = "SELECT token, username, role, expires_at_utc FROM portal_sessions WHERE token = @t LIMIT 1";
        cmd.Parameters.AddWithValue("@t", token);

        using var reader = await cmd.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        if (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            var expires = DateTime.Parse(reader.GetString(3));
            if (expires > DateTime.UtcNow)
            {
                return new PortalSession(reader.GetString(0), reader.GetString(1), reader.GetString(2), expires);
            }
            // Expired -> clean up
            await RevokeSessionAsync(token, cancellationToken).ConfigureAwait(false);
        }
        return null;
    }

    public async Task RevokeSessionAsync(string token, CancellationToken cancellationToken = default)
    {
        if (_connection == null) return;
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "DELETE FROM portal_sessions WHERE token = @t";
        cmd.Parameters.AddWithValue("@t", token);
        await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task LogAuditAsync(string action, string username, string details, CancellationToken cancellationToken = default)
    {
        if (_connection == null) await InitializeAsync(cancellationToken).ConfigureAwait(false);

        using var cmd = _connection!.CreateCommand();
        cmd.CommandText = "INSERT INTO portal_audit_logs (action, username, details, timestamp_utc) VALUES (@a, @u, @d, @t)";
        cmd.Parameters.AddWithValue("@a", action);
        cmd.Parameters.AddWithValue("@u", username);
        cmd.Parameters.AddWithValue("@d", details);
        cmd.Parameters.AddWithValue("@t", DateTime.UtcNow.ToString("O"));

        await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<PortalAuditLog>> GetAuditLogsAsync(int limit = 50, CancellationToken cancellationToken = default)
    {
        if (_connection == null) await InitializeAsync(cancellationToken).ConfigureAwait(false);

        var list = new List<PortalAuditLog>();
        using var cmd = _connection!.CreateCommand();
        cmd.CommandText = "SELECT id, action, username, details, timestamp_utc FROM portal_audit_logs ORDER BY id DESC LIMIT @limit";
        cmd.Parameters.AddWithValue("@limit", limit);

        using var reader = await cmd.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            list.Add(new PortalAuditLog(
                reader.GetInt64(0),
                reader.GetString(1),
                reader.GetString(2),
                reader.GetString(3),
                DateTime.Parse(reader.GetString(4))
            ));
        }
        return list;
    }

    public static string HashPassword(string password)
    {
        byte[] salt = RandomNumberGenerator.GetBytes(16);
        byte[] hash = Rfc2898DeriveBytes.Pbkdf2(
            Encoding.UTF8.GetBytes(password),
            salt,
            iterations: 100_000,
            hashAlgorithm: HashAlgorithmName.SHA256,
            outputLength: 32
        );
        return $"{Convert.ToBase64String(salt)}:{Convert.ToBase64String(hash)}";
    }

    public static bool VerifyPassword(string password, string storedHash)
    {
        var parts = storedHash.Split(':');
        if (parts.Length != 2) return false;

        byte[] salt = Convert.FromBase64String(parts[0]);
        byte[] expectedHash = Convert.FromBase64String(parts[1]);

        byte[] actualHash = Rfc2898DeriveBytes.Pbkdf2(
            Encoding.UTF8.GetBytes(password),
            salt,
            iterations: 100_000,
            hashAlgorithm: HashAlgorithmName.SHA256,
            outputLength: 32
        );

        return CryptographicOperations.FixedTimeEquals(actualHash, expectedHash);
    }

    public async ValueTask DisposeAsync()
    {
        if (_connection != null)
        {
            await _connection.DisposeAsync().ConfigureAwait(false);
            _connection = null;
        }
    }
}
