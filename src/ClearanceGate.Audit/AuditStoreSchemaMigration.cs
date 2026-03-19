using Microsoft.Data.Sqlite;

namespace ClearanceGate.Audit;

internal sealed class AuditStoreSchemaMigration(
    int targetVersion,
    Func<SqliteConnection, CancellationToken, Task> applyAsync)
{
    public int TargetVersion { get; } = targetVersion;

    public Task ApplyAsync(SqliteConnection connection, CancellationToken cancellationToken) =>
        applyAsync(connection, cancellationToken);
}
