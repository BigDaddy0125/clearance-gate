using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace ClearanceGate.Api.Tests;

public sealed class AuditStoreSchemaTests
{
    [Fact]
    public async Task InitializeAsync_StampsCurrentSchemaVersionOnFreshDatabase()
    {
        using var harness = CreateHarness();
        var initializer = CreateInitializer(harness.DatabasePath);

        await initializer.InitializeAsync(CancellationToken.None);

        await using var connection = new SqliteConnection($"Data Source={harness.DatabasePath}");
        await connection.OpenAsync();

        var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT value
            FROM schema_metadata
            WHERE key = $key;
            """;
        command.Parameters.AddWithValue("$key", ClearanceGate.Audit.AuditStoreSchema.SchemaVersionKey);

        var result = await command.ExecuteScalarAsync();

        Assert.Equal(ClearanceGate.Audit.AuditStoreSchema.CurrentVersion.ToString(), Convert.ToString(result));
    }

    [Fact]
    public async Task InitializeAsync_UpgradesLegacyUnversionedDatabaseAndPreservesData()
    {
        using var harness = CreateHarness();
        SeedLegacyUnversionedDatabase(harness.DatabasePath);
        var initializer = CreateInitializer(harness.DatabasePath);

        await initializer.InitializeAsync(CancellationToken.None);

        await using var connection = new SqliteConnection($"Data Source={harness.DatabasePath}");
        await connection.OpenAsync();

        var versionCommand = connection.CreateCommand();
        versionCommand.CommandText =
            """
            SELECT value
            FROM schema_metadata
            WHERE key = $key;
            """;
        versionCommand.Parameters.AddWithValue("$key", ClearanceGate.Audit.AuditStoreSchema.SchemaVersionKey);

        var version = await versionCommand.ExecuteScalarAsync();

        var dataCommand = connection.CreateCommand();
        dataCommand.CommandText = "SELECT COUNT(*) FROM decisions WHERE decision_id = 'legacy-decision';";
        var dataCount = await dataCommand.ExecuteScalarAsync();

        var fingerprintColumnCommand = connection.CreateCommand();
        fingerprintColumnCommand.CommandText = "PRAGMA table_info(decisions);";
        await using var columnReader = await fingerprintColumnCommand.ExecuteReaderAsync();
        var fingerprintColumnExists = false;
        while (await columnReader.ReadAsync())
        {
            if (string.Equals(columnReader.GetString(1), "request_fingerprint", StringComparison.Ordinal))
            {
                fingerprintColumnExists = true;
                break;
            }
        }

        Assert.Equal(ClearanceGate.Audit.AuditStoreSchema.CurrentVersion.ToString(), Convert.ToString(version));
        Assert.Equal(1L, Convert.ToInt64(dataCount));
        Assert.True(fingerprintColumnExists);
    }

    [Fact]
    public void ApplicationStartup_RejectsUnsupportedAuditSchemaVersion()
    {
        using var harness = CreateHarness();
        SeedUnsupportedSchemaVersion(harness.DatabasePath, 999);
        using var factory = new ClearanceGateApiFactory(harness.DatabasePath);

        var exception = Assert.ThrowsAny<Exception>(() => factory.CreateClient());

        Assert.Contains("Unsupported audit store schema version", exception.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public void ApplicationStartup_RejectsNonIntegerAuditSchemaVersion()
    {
        using var harness = CreateHarness();
        SeedSchemaVersionValue(harness.DatabasePath, "v-next");
        using var factory = new ClearanceGateApiFactory(harness.DatabasePath);

        var exception = Assert.ThrowsAny<Exception>(() => factory.CreateClient());

        Assert.Contains("schema version 'v-next' is not a valid integer", exception.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task InitializeAsync_RejectsIncompleteCurrentSchema()
    {
        using var harness = CreateHarness();
        SeedIncompleteCurrentSchema(harness.DatabasePath);
        var initializer = CreateInitializer(harness.DatabasePath);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => initializer.InitializeAsync(CancellationToken.None));

        Assert.Contains("Required table 'decision_timeline' is missing", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task InitializeAsync_RejectsCurrentSchemaMissingRequestFingerprintColumn()
    {
        using var harness = CreateHarness();
        SeedCurrentSchemaWithoutRequestFingerprint(harness.DatabasePath);
        var initializer = CreateInitializer(harness.DatabasePath);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => initializer.InitializeAsync(CancellationToken.None));

        Assert.Contains("Required column 'decisions.request_fingerprint' is missing", exception.Message, StringComparison.Ordinal);
    }

    private static ClearanceGate.Audit.SqliteAuditStoreInitializer CreateInitializer(string databasePath)
    {
        var options = Options.Create(new ClearanceGate.Audit.AuditStoreOptions
        {
            ConnectionString = $"Data Source={databasePath}",
        });

        return new ClearanceGate.Audit.SqliteAuditStoreInitializer(
            options,
            NullLogger<ClearanceGate.Audit.SqliteAuditStoreInitializer>.Instance);
    }

    private static void SeedLegacyUnversionedDatabase(string databasePath)
    {
        var connectionStringBuilder = new SqliteConnectionStringBuilder($"Data Source={databasePath}");
        var directory = Path.GetDirectoryName(Path.GetFullPath(connectionStringBuilder.DataSource));
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        using var connection = new SqliteConnection(connectionStringBuilder.ToString());
        connection.Open();

        var command = connection.CreateCommand();
        command.CommandText =
            """
            CREATE TABLE decisions (
                request_id TEXT NOT NULL PRIMARY KEY,
                decision_id TEXT NOT NULL UNIQUE,
                profile TEXT NOT NULL,
                owner TEXT NOT NULL,
                acknowledger_id TEXT NULL,
                outcome TEXT NOT NULL,
                clearance_state TEXT NOT NULL,
                evidence_id TEXT NOT NULL,
                summary TEXT NOT NULL,
                kernel_version TEXT NOT NULL,
                policy_version TEXT NOT NULL
            );

            CREATE TABLE decision_constraints (
                decision_id TEXT NOT NULL,
                constraint_id TEXT NOT NULL,
                PRIMARY KEY (decision_id, constraint_id),
                FOREIGN KEY (decision_id) REFERENCES decisions(decision_id) ON DELETE CASCADE
            );

            CREATE TABLE decision_timeline (
                timeline_id INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
                decision_id TEXT NOT NULL,
                state TEXT NOT NULL,
                timestamp TEXT NOT NULL,
                FOREIGN KEY (decision_id) REFERENCES decisions(decision_id) ON DELETE CASCADE
            );

            INSERT INTO decisions (
                request_id,
                decision_id,
                profile,
                owner,
                acknowledger_id,
                outcome,
                clearance_state,
                evidence_id,
                summary,
                kernel_version,
                policy_version)
            VALUES (
                'legacy-request',
                'legacy-decision',
                'itops_deployment_v1',
                'alice',
                NULL,
                'REQUIRE_ACK',
                'AWAITING_ACK',
                'evidence:legacy-decision',
                'legacy summary',
                'kernel-v1',
                'policy-v1');
            """;
        command.ExecuteNonQuery();
    }

    private static void SeedUnsupportedSchemaVersion(string databasePath, int schemaVersion)
    {
        SeedSchemaVersionValue(databasePath, schemaVersion.ToString());
    }

    private static void SeedSchemaVersionValue(string databasePath, string schemaVersion)
    {
        var connectionStringBuilder = new SqliteConnectionStringBuilder($"Data Source={databasePath}");
        var directory = Path.GetDirectoryName(Path.GetFullPath(connectionStringBuilder.DataSource));
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        using var connection = new SqliteConnection(connectionStringBuilder.ToString());
        connection.Open();

        var command = connection.CreateCommand();
        command.CommandText =
            """
            CREATE TABLE IF NOT EXISTS schema_metadata (
                key TEXT NOT NULL PRIMARY KEY,
                value TEXT NOT NULL
            );

            INSERT INTO schema_metadata (key, value)
            VALUES ($key, $value)
            ON CONFLICT(key) DO UPDATE SET value = excluded.value;
            """;
        command.Parameters.AddWithValue("$key", ClearanceGate.Audit.AuditStoreSchema.SchemaVersionKey);
        command.Parameters.AddWithValue("$value", schemaVersion);
        command.ExecuteNonQuery();
    }

    private static void SeedIncompleteCurrentSchema(string databasePath)
    {
        var connectionStringBuilder = new SqliteConnectionStringBuilder($"Data Source={databasePath}");
        var directory = Path.GetDirectoryName(Path.GetFullPath(connectionStringBuilder.DataSource));
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        using var connection = new SqliteConnection(connectionStringBuilder.ToString());
        connection.Open();

        var command = connection.CreateCommand();
        command.CommandText =
            """
            CREATE TABLE schema_metadata (
                key TEXT NOT NULL PRIMARY KEY,
                value TEXT NOT NULL
            );

            INSERT INTO schema_metadata (key, value)
            VALUES ($key, $value);

            CREATE TABLE decisions (
                request_id TEXT NOT NULL PRIMARY KEY,
                decision_id TEXT NOT NULL UNIQUE,
                profile TEXT NOT NULL,
                owner TEXT NOT NULL,
                request_fingerprint TEXT NULL,
                acknowledger_id TEXT NULL,
                outcome TEXT NOT NULL,
                clearance_state TEXT NOT NULL,
                evidence_id TEXT NOT NULL,
                summary TEXT NOT NULL,
                kernel_version TEXT NOT NULL,
                policy_version TEXT NOT NULL
            );

            CREATE TABLE decision_constraints (
                decision_id TEXT NOT NULL,
                constraint_id TEXT NOT NULL,
                PRIMARY KEY (decision_id, constraint_id)
            );
            """;
        command.Parameters.AddWithValue("$key", ClearanceGate.Audit.AuditStoreSchema.SchemaVersionKey);
        command.Parameters.AddWithValue("$value", ClearanceGate.Audit.AuditStoreSchema.CurrentVersion.ToString());
        command.ExecuteNonQuery();
    }

    private static void SeedCurrentSchemaWithoutRequestFingerprint(string databasePath)
    {
        var connectionStringBuilder = new SqliteConnectionStringBuilder($"Data Source={databasePath}");
        var directory = Path.GetDirectoryName(Path.GetFullPath(connectionStringBuilder.DataSource));
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        using var connection = new SqliteConnection(connectionStringBuilder.ToString());
        connection.Open();

        var command = connection.CreateCommand();
        command.CommandText =
            """
            CREATE TABLE schema_metadata (
                key TEXT NOT NULL PRIMARY KEY,
                value TEXT NOT NULL
            );

            INSERT INTO schema_metadata (key, value)
            VALUES ($key, $value);

            CREATE TABLE decisions (
                request_id TEXT NOT NULL PRIMARY KEY,
                decision_id TEXT NOT NULL UNIQUE,
                profile TEXT NOT NULL,
                owner TEXT NOT NULL,
                acknowledger_id TEXT NULL,
                outcome TEXT NOT NULL,
                clearance_state TEXT NOT NULL,
                evidence_id TEXT NOT NULL,
                summary TEXT NOT NULL,
                kernel_version TEXT NOT NULL,
                policy_version TEXT NOT NULL
            );

            CREATE TABLE decision_constraints (
                decision_id TEXT NOT NULL,
                constraint_id TEXT NOT NULL,
                PRIMARY KEY (decision_id, constraint_id)
            );

            CREATE TABLE decision_timeline (
                timeline_id INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
                decision_id TEXT NOT NULL,
                state TEXT NOT NULL,
                timestamp TEXT NOT NULL
            );
            """;
        command.Parameters.AddWithValue("$key", ClearanceGate.Audit.AuditStoreSchema.SchemaVersionKey);
        command.Parameters.AddWithValue("$value", ClearanceGate.Audit.AuditStoreSchema.CurrentVersion.ToString());
        command.ExecuteNonQuery();
    }

    private static TemporaryDatabaseHarness CreateHarness()
    {
        var path = Path.Combine(Path.GetTempPath(), $"clearancegate-schema-tests-{Guid.NewGuid():N}.db");
        return new TemporaryDatabaseHarness(path);
    }

    private sealed class TemporaryDatabaseHarness : IDisposable
    {
        public TemporaryDatabaseHarness(string databasePath)
        {
            DatabasePath = databasePath;
        }

        public string DatabasePath { get; }

        public void Dispose()
        {
            TryDelete(DatabasePath);
            TryDelete($"{DatabasePath}-shm");
            TryDelete($"{DatabasePath}-wal");
        }

        private static void TryDelete(string path)
        {
            try
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
            catch (IOException)
            {
            }
        }
    }
}
