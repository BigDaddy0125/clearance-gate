using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ClearanceGate.Audit;

public sealed class SqliteAuditStoreInitializer(
    IOptions<AuditStoreOptions> options,
    ILogger<SqliteAuditStoreInitializer> logger) : IAuditStoreInitializer
{
    private static class LogEvents
    {
        public static readonly EventId InitializationStarted = new(2000, nameof(InitializationStarted));
        public static readonly EventId DirectoryReady = new(2001, nameof(DirectoryReady));
        public static readonly EventId InitializationCompleted = new(2002, nameof(InitializationCompleted));
    }

    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        var connectionString = options.Value.ConnectionString;
        var connectionStringBuilder = new SqliteConnectionStringBuilder(connectionString);
        var dataSource = connectionStringBuilder.DataSource;

        logger.LogInformation(
            LogEvents.InitializationStarted,
            "Initializing audit store. DataSource={DataSource}",
            string.IsNullOrWhiteSpace(dataSource) ? "<empty>" : dataSource);

        if (!string.IsNullOrWhiteSpace(dataSource))
        {
            var directory = Path.GetDirectoryName(Path.GetFullPath(dataSource));
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
                logger.LogInformation(LogEvents.DirectoryReady, "Ensured audit store directory exists. Directory={Directory}", directory);
            }
        }

        await using var connection = new SqliteConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        await ConfigurePragmasAsync(connection, cancellationToken);
        await EnsureSchemaMetadataAsync(connection, cancellationToken);

        var schemaVersion = await ReadSchemaVersionAsync(connection, cancellationToken);
        if (schemaVersion is > AuditStoreSchema.CurrentVersion)
        {
            throw new InvalidOperationException(
                $"Unsupported audit store schema version '{schemaVersion}'. Expected '{AuditStoreSchema.CurrentVersion}'.");
        }

        await ApplyPendingMigrationsAsync(connection, schemaVersion, cancellationToken);
        await EnsureRequiredTablesExistAsync(connection, cancellationToken);

        logger.LogInformation(
            LogEvents.InitializationCompleted,
            "Audit store initialization completed. SchemaVersion={SchemaVersion} CurrentVersion={CurrentVersion}",
            schemaVersion ?? AuditStoreSchema.CurrentVersion,
            AuditStoreSchema.CurrentVersion);
    }

    private static async Task ConfigurePragmasAsync(
        SqliteConnection connection,
        CancellationToken cancellationToken)
    {
        var command = connection.CreateCommand();
        command.CommandText = "PRAGMA journal_mode = WAL;";
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task EnsureSchemaMetadataAsync(
        SqliteConnection connection,
        CancellationToken cancellationToken)
    {
        var command = connection.CreateCommand();
        command.CommandText =
            """
            CREATE TABLE IF NOT EXISTS schema_metadata (
                key TEXT NOT NULL PRIMARY KEY,
                value TEXT NOT NULL
            );
            """;
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task<int?> ReadSchemaVersionAsync(
        SqliteConnection connection,
        CancellationToken cancellationToken)
    {
        var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT value
            FROM schema_metadata
            WHERE key = $key;
            """;
        command.Parameters.AddWithValue("$key", AuditStoreSchema.SchemaVersionKey);

        var result = await command.ExecuteScalarAsync(cancellationToken);
        if (result is null || result is DBNull)
        {
            return null;
        }

        var rawValue = Convert.ToString(result);
        if (int.TryParse(rawValue, out var parsedVersion))
        {
            return parsedVersion;
        }

        throw new InvalidOperationException(
            $"Audit store schema version '{rawValue}' is not a valid integer.");
    }

    private static async Task ApplyPendingMigrationsAsync(
        SqliteConnection connection,
        int? currentVersion,
        CancellationToken cancellationToken)
    {
        var effectiveVersion = currentVersion ?? 0;
        foreach (var migration in Migrations.Where(migration => migration.TargetVersion > effectiveVersion))
        {
            await migration.ApplyAsync(connection, cancellationToken);
            await WriteSchemaVersionAsync(connection, migration.TargetVersion, cancellationToken);
        }
    }

    private static async Task WriteSchemaVersionAsync(
        SqliteConnection connection,
        int schemaVersion,
        CancellationToken cancellationToken)
    {
        var command = connection.CreateCommand();
        command.CommandText =
            """
            INSERT INTO schema_metadata (key, value)
            VALUES ($key, $value)
            ON CONFLICT(key) DO UPDATE SET value = excluded.value;
            """;
        command.Parameters.AddWithValue("$key", AuditStoreSchema.SchemaVersionKey);
        command.Parameters.AddWithValue("$value", schemaVersion.ToString());
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task EnsureRequiredTablesExistAsync(
        SqliteConnection connection,
        CancellationToken cancellationToken)
    {
        foreach (var tableName in RequiredTables)
        {
            var command = connection.CreateCommand();
            command.CommandText =
                """
                SELECT 1
                FROM sqlite_master
                WHERE type = 'table'
                  AND name = $name;
                """;
            command.Parameters.AddWithValue("$name", tableName);

            var exists = await command.ExecuteScalarAsync(cancellationToken) is not null;
            if (!exists)
            {
                throw new InvalidOperationException(
                    $"Audit store schema is incomplete. Required table '{tableName}' is missing.");
            }
        }
    }

    private static readonly string[] RequiredTables =
    [
        "schema_metadata",
        "decisions",
        "decision_constraints",
        "decision_timeline",
    ];

    private static readonly AuditStoreSchemaMigration[] Migrations =
    [
        new(
            1,
            async (connection, cancellationToken) =>
            {
                var command = connection.CreateCommand();
                command.CommandText =
                    """
                    CREATE TABLE IF NOT EXISTS decisions (
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

                    CREATE TABLE IF NOT EXISTS decision_constraints (
                        decision_id TEXT NOT NULL,
                        constraint_id TEXT NOT NULL,
                        PRIMARY KEY (decision_id, constraint_id),
                        FOREIGN KEY (decision_id) REFERENCES decisions(decision_id) ON DELETE CASCADE
                    );

                    CREATE TABLE IF NOT EXISTS decision_timeline (
                        timeline_id INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
                        decision_id TEXT NOT NULL,
                        state TEXT NOT NULL,
                        timestamp TEXT NOT NULL,
                        FOREIGN KEY (decision_id) REFERENCES decisions(decision_id) ON DELETE CASCADE
                    );
                    """;
                await command.ExecuteNonQueryAsync(cancellationToken);
            }),
    ];
}
