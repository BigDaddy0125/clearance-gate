using Microsoft.Data.Sqlite;
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
    public void ApplicationStartup_RejectsUnsupportedAuditSchemaVersion()
    {
        using var harness = CreateHarness();
        SeedUnsupportedSchemaVersion(harness.DatabasePath, 999);
        using var factory = new ClearanceGateApiFactory(harness.DatabasePath);

        var exception = Assert.ThrowsAny<Exception>(() => factory.CreateClient());

        Assert.Contains("Unsupported audit store schema version", exception.ToString(), StringComparison.Ordinal);
    }

    private static ClearanceGate.Audit.SqliteAuditStoreInitializer CreateInitializer(string databasePath)
    {
        var options = Options.Create(new ClearanceGate.Audit.AuditStoreOptions
        {
            ConnectionString = $"Data Source={databasePath}",
        });

        return new ClearanceGate.Audit.SqliteAuditStoreInitializer(options);
    }

    private static void SeedUnsupportedSchemaVersion(string databasePath, int schemaVersion)
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
        command.Parameters.AddWithValue("$value", schemaVersion.ToString());
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
