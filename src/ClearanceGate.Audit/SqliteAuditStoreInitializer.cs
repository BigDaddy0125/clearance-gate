using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Options;

namespace ClearanceGate.Audit;

public sealed class SqliteAuditStoreInitializer(
    IOptions<AuditStoreOptions> options) : IAuditStoreInitializer
{
    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        var connectionString = options.Value.ConnectionString;
        var connectionStringBuilder = new SqliteConnectionStringBuilder(connectionString);

        if (!string.IsNullOrWhiteSpace(connectionStringBuilder.DataSource))
        {
            var directory = Path.GetDirectoryName(Path.GetFullPath(connectionStringBuilder.DataSource));
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }
        }

        await using var connection = new SqliteConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        var command = connection.CreateCommand();
        command.CommandText =
            """
            PRAGMA journal_mode = WAL;

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
    }
}
