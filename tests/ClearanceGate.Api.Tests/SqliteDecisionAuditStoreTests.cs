using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Options;
using Xunit;

namespace ClearanceGate.Api.Tests;

public sealed class SqliteDecisionAuditStoreTests
{
    [Fact]
    public async Task SaveAuthorization_RequestConflictReturnsCanonicalRecordWithoutDuplicateChildRows()
    {
        using var harness = CreateHarness();
        var store = await CreateStoreAsync(harness.DatabasePath);

        var canonical = CreateRecord(
            requestId: "req-store-1",
            decisionId: "dec-store-1",
            outcome: "REQUIRE_ACK",
            state: "AWAITING_ACK",
            constraints: ["OWNER_REQUIRED", "RISK_ACK_REQUIRED"]);
        var conflicting = CreateRecord(
            requestId: "req-store-1",
            decisionId: "dec-store-2",
            outcome: "BLOCK",
            state: "INFO_INSUFFICIENT",
            constraints: ["SOURCE_REQUIRED"]);

        var first = store.SaveAuthorization(canonical);
        var second = store.SaveAuthorization(conflicting);

        Assert.Equal("dec-store-1", first.DecisionId);
        Assert.Equal("dec-store-1", second.DecisionId);

        await using var connection = new SqliteConnection($"Data Source={harness.DatabasePath}");
        await connection.OpenAsync();

        var decisions = await ExecuteScalarAsync(connection, "SELECT COUNT(*) FROM decisions;");
        var constraints = await ExecuteScalarAsync(connection, "SELECT COUNT(*) FROM decision_constraints;");
        var timeline = await ExecuteScalarAsync(connection, "SELECT COUNT(*) FROM decision_timeline;");
        var loserConstraints = await ExecuteScalarAsync(connection, "SELECT COUNT(*) FROM decision_constraints WHERE decision_id = 'dec-store-2';");
        var loserTimeline = await ExecuteScalarAsync(connection, "SELECT COUNT(*) FROM decision_timeline WHERE decision_id = 'dec-store-2';");

        Assert.Equal(1L, decisions);
        Assert.Equal(2L, constraints);
        Assert.Equal(1L, timeline);
        Assert.Equal(0L, loserConstraints);
        Assert.Equal(0L, loserTimeline);
    }

    [Fact]
    public async Task SaveAcknowledgment_ReplayDoesNotAppendDuplicateTimeline()
    {
        using var harness = CreateHarness();
        var store = await CreateStoreAsync(harness.DatabasePath);
        var record = CreateRecord(
            requestId: "req-store-2",
            decisionId: "dec-store-2",
            outcome: "REQUIRE_ACK",
            state: "AWAITING_ACK",
            constraints: ["RISK_ACK_REQUIRED"]);

        store.SaveAuthorization(record);

        var firstAck = store.SaveAcknowledgment(
            "dec-store-2",
            "alice",
            "PROCEED",
            "AUTHORIZED",
            "Authorization acknowledged by designated authority.",
            "2026-03-18T10:05:00Z");
        var secondAck = store.SaveAcknowledgment(
            "dec-store-2",
            "alice",
            "PROCEED",
            "AUTHORIZED",
            "Authorization acknowledged by designated authority.",
            "2026-03-18T10:05:00Z");

        Assert.Equal(ClearanceGate.Audit.AcknowledgmentWriteStatus.Applied, firstAck.Status);
        Assert.Equal(ClearanceGate.Audit.AcknowledgmentWriteStatus.AlreadyApplied, secondAck.Status);
        Assert.NotNull(secondAck.Record);
        Assert.Equal(new[] { "AWAITING_ACK", "AUTHORIZED" }, secondAck.Record.Timeline.Select(item => item.State));

        await using var connection = new SqliteConnection($"Data Source={harness.DatabasePath}");
        await connection.OpenAsync();
        var timeline = await ExecuteScalarAsync(connection, "SELECT COUNT(*) FROM decision_timeline WHERE decision_id = 'dec-store-2';");
        Assert.Equal(2L, timeline);
    }

    [Fact]
    public async Task SaveAcknowledgment_InvalidStateDoesNotMutateStoredRecord()
    {
        using var harness = CreateHarness();
        var store = await CreateStoreAsync(harness.DatabasePath);
        var record = CreateRecord(
            requestId: "req-store-3",
            decisionId: "dec-store-3",
            outcome: "BLOCK",
            state: "INFO_INSUFFICIENT",
            constraints: ["SOURCE_REQUIRED"]);

        store.SaveAuthorization(record);

        var ack = store.SaveAcknowledgment(
            "dec-store-3",
            "alice",
            "PROCEED",
            "AUTHORIZED",
            "Authorization acknowledged by designated authority.",
            "2026-03-18T10:05:00Z");

        Assert.Equal(ClearanceGate.Audit.AcknowledgmentWriteStatus.InvalidState, ack.Status);
        Assert.NotNull(ack.Record);
        Assert.Equal("BLOCK", ack.Record.Outcome);
        Assert.Equal("INFO_INSUFFICIENT", ack.Record.ClearanceState);
        Assert.Single(ack.Record.Timeline);
        Assert.Null(ack.Record.AcknowledgerId);

        await using var connection = new SqliteConnection($"Data Source={harness.DatabasePath}");
        await connection.OpenAsync();
        var timeline = await ExecuteScalarAsync(connection, "SELECT COUNT(*) FROM decision_timeline WHERE decision_id = 'dec-store-3';");
        Assert.Equal(1L, timeline);
    }

    private static async Task<ClearanceGate.Audit.SqliteDecisionAuditStore> CreateStoreAsync(string databasePath)
    {
        var options = Options.Create(new ClearanceGate.Audit.AuditStoreOptions
        {
            ConnectionString = $"Data Source={databasePath}",
        });

        var initializer = new ClearanceGate.Audit.SqliteAuditStoreInitializer(options);
        await initializer.InitializeAsync(CancellationToken.None);
        return new ClearanceGate.Audit.SqliteDecisionAuditStore(options);
    }

    private static ClearanceGate.Audit.DecisionAuditRecord CreateRecord(
        string requestId,
        string decisionId,
        string outcome,
        string state,
        IReadOnlyList<string> constraints)
    {
        var record = new ClearanceGate.Audit.DecisionAuditRecord
        {
            RequestId = requestId,
            DecisionId = decisionId,
            Profile = "itops_deployment_v1",
            Owner = "alice",
            Outcome = outcome,
            ClearanceState = state,
            EvidenceId = $"evidence:{decisionId}",
            Summary = "summary",
            KernelVersion = "0.1.0",
            PolicyVersion = "itops_deployment_v1",
        };

        record.ConstraintsApplied.AddRange(constraints);
        record.Timeline.Add(new ClearanceGate.Audit.DecisionAuditTransition(state, "2026-03-18T10:00:00Z"));
        return record;
    }

    private static async Task<long> ExecuteScalarAsync(SqliteConnection connection, string sql)
    {
        var command = connection.CreateCommand();
        command.CommandText = sql;
        var result = await command.ExecuteScalarAsync();
        return Convert.ToInt64(result);
    }

    private static TemporaryDatabaseHarness CreateHarness()
    {
        var path = Path.Combine(Path.GetTempPath(), $"clearancegate-store-tests-{Guid.NewGuid():N}.db");
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
