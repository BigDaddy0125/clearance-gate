using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Options;

namespace ClearanceGate.Audit;

public sealed class SqliteDecisionAuditStore(
    IOptions<AuditStoreOptions> options) : IDecisionAuditStore
{
    private const int SqliteBusy = 5;
    private const int SqliteLocked = 6;
    private const int MaxWriteAttempts = 5;

    private readonly string connectionString = options.Value.ConnectionString;

    public DecisionAuditRecord? GetByRequestId(string requestId)
    {
        using var connection = OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT
                request_id,
                decision_id,
                profile,
                owner,
                request_fingerprint,
                acknowledger_id,
                outcome,
                clearance_state,
                evidence_id,
                summary,
                kernel_version,
                policy_version
            FROM decisions
            WHERE request_id = $requestId
            LIMIT 1;
            """;
        command.Parameters.AddWithValue("$requestId", requestId);

        using var reader = command.ExecuteReader();
        if (!reader.Read())
        {
            return null;
        }

        var record = MapRecord(reader);
        LoadConstraints(connection, record);
        LoadTimeline(connection, record);
        return record;
    }

    public DecisionAuditRecord? GetByDecisionId(string decisionId)
    {
        using var connection = OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT
                request_id,
                decision_id,
                profile,
                owner,
                request_fingerprint,
                acknowledger_id,
                outcome,
                clearance_state,
                evidence_id,
                summary,
                kernel_version,
                policy_version
            FROM decisions
            WHERE decision_id = $decisionId
            LIMIT 1;
            """;
        command.Parameters.AddWithValue("$decisionId", decisionId);

        using var reader = command.ExecuteReader();
        if (!reader.Read())
        {
            return null;
        }

        var record = MapRecord(reader);
        LoadConstraints(connection, record);
        LoadTimeline(connection, record);
        return record;
    }

    public DecisionAuditRecord SaveAuthorization(DecisionAuditRecord record)
    {
        return RetryWrite(() =>
        {
            using var connection = OpenConnection();
            using var transaction = connection.BeginTransaction();

            int insertedDecisionCount;
            using (var insert = connection.CreateCommand())
            {
                insert.Transaction = transaction;
                insert.CommandText =
                    """
                    INSERT INTO decisions (
                        request_id,
                        decision_id,
                        profile,
                        owner,
                        request_fingerprint,
                        acknowledger_id,
                        outcome,
                        clearance_state,
                        evidence_id,
                        summary,
                        kernel_version,
                        policy_version
                    )
                    VALUES (
                        $requestId,
                        $decisionId,
                        $profile,
                        $owner,
                        $requestFingerprint,
                        $acknowledgerId,
                        $outcome,
                        $clearanceState,
                        $evidenceId,
                        $summary,
                        $kernelVersion,
                        $policyVersion
                    )
                    ON CONFLICT(request_id) DO NOTHING;
                    """;
                BindDecision(insert, record);
                insertedDecisionCount = insert.ExecuteNonQuery();
            }

            if (insertedDecisionCount == 0)
            {
                transaction.Commit();
                return ReadExistingRequestRecord(record.RequestId);
            }

            using (var insertConstraint = connection.CreateCommand())
            {
                insertConstraint.Transaction = transaction;
                insertConstraint.CommandText =
                    """
                    INSERT INTO decision_constraints (decision_id, constraint_id)
                    VALUES ($decisionId, $constraintId)
                    ON CONFLICT(decision_id, constraint_id) DO NOTHING;
                    """;

                var decisionIdParameter = insertConstraint.Parameters.Add("$decisionId", SqliteType.Text);
                var constraintParameter = insertConstraint.Parameters.Add("$constraintId", SqliteType.Text);
                decisionIdParameter.Value = record.DecisionId;

                foreach (var constraint in record.ConstraintsApplied)
                {
                    constraintParameter.Value = constraint;
                    insertConstraint.ExecuteNonQuery();
                }
            }

            using (var insertTimeline = connection.CreateCommand())
            {
                insertTimeline.Transaction = transaction;
                insertTimeline.CommandText =
                    """
                    INSERT INTO decision_timeline (decision_id, state, timestamp)
                    VALUES ($decisionId, $state, $timestamp);
                    """;

                var decisionIdParameter = insertTimeline.Parameters.Add("$decisionId", SqliteType.Text);
                var stateParameter = insertTimeline.Parameters.Add("$state", SqliteType.Text);
                var timestampParameter = insertTimeline.Parameters.Add("$timestamp", SqliteType.Text);
                decisionIdParameter.Value = record.DecisionId;

                foreach (var transition in record.Timeline)
                {
                    stateParameter.Value = transition.State;
                    timestampParameter.Value = transition.Timestamp;
                    insertTimeline.ExecuteNonQuery();
                }
            }

            var existing = GetByRequestIdWithinTransaction(connection, transaction, record.RequestId);
            if (existing is not null)
            {
                transaction.Commit();
                return existing;
            }

            throw new InvalidOperationException($"Authorization record '{record.RequestId}' could not be stored.");
        });
    }

    public AcknowledgmentWriteResult SaveAcknowledgment(
        string decisionId,
        string acknowledgerId,
        string outcome,
        string state,
        string summary,
        string timestamp)
    {
        return RetryWrite(() =>
        {
            using var connection = OpenConnection();
            using var transaction = connection.BeginTransaction();

            var record = GetByDecisionIdWithinTransaction(connection, transaction, decisionId);
            if (record is null)
            {
                return new AcknowledgmentWriteResult(AcknowledgmentWriteStatus.NotFound, null);
            }

            if (record.ClearanceState == "AUTHORIZED" &&
                string.Equals(record.AcknowledgerId, acknowledgerId, StringComparison.Ordinal))
            {
                transaction.Commit();
                return new AcknowledgmentWriteResult(AcknowledgmentWriteStatus.AlreadyApplied, record);
            }

            if (record.ClearanceState != "AWAITING_ACK" ||
                !record.ConstraintsApplied.Contains("RISK_ACK_REQUIRED", StringComparer.Ordinal))
            {
                transaction.Commit();
                return new AcknowledgmentWriteResult(AcknowledgmentWriteStatus.InvalidState, record);
            }

            using (var update = connection.CreateCommand())
            {
                update.Transaction = transaction;
                update.CommandText =
                    """
                    UPDATE decisions
                    SET acknowledger_id = $acknowledgerId,
                        outcome = $outcome,
                        clearance_state = $state,
                        summary = $summary
                    WHERE decision_id = $decisionId;
                    """;
                update.Parameters.AddWithValue("$acknowledgerId", acknowledgerId);
                update.Parameters.AddWithValue("$outcome", outcome);
                update.Parameters.AddWithValue("$state", state);
                update.Parameters.AddWithValue("$summary", summary);
                update.Parameters.AddWithValue("$decisionId", decisionId);
                update.ExecuteNonQuery();
            }

            using (var appendTimeline = connection.CreateCommand())
            {
                appendTimeline.Transaction = transaction;
                appendTimeline.CommandText =
                    """
                    INSERT INTO decision_timeline (decision_id, state, timestamp)
                    VALUES ($decisionId, $state, $timestamp);
                    """;
                appendTimeline.Parameters.AddWithValue("$decisionId", decisionId);
                appendTimeline.Parameters.AddWithValue("$state", state);
                appendTimeline.Parameters.AddWithValue("$timestamp", timestamp);
                appendTimeline.ExecuteNonQuery();
            }

            var updated = GetByDecisionIdWithinTransaction(connection, transaction, decisionId)
                ?? throw new InvalidOperationException($"Acknowledged decision '{decisionId}' could not be reloaded.");

            transaction.Commit();
            return new AcknowledgmentWriteResult(AcknowledgmentWriteStatus.Applied, updated);
        });
    }

    private SqliteConnection OpenConnection()
    {
        var connection = new SqliteConnection(connectionString);
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText =
            """
            PRAGMA busy_timeout = 5000;
            PRAGMA foreign_keys = ON;
            """;
        command.ExecuteNonQuery();
        return connection;
    }

    private DecisionAuditRecord ReadExistingRequestRecord(string requestId)
    {
        for (var attempt = 1; attempt <= MaxWriteAttempts; attempt++)
        {
            var existing = GetByRequestId(requestId);
            if (existing is not null)
            {
                return existing;
            }

            Thread.Sleep(25 * attempt);
        }

        throw new InvalidOperationException($"Authorization record '{requestId}' could not be reloaded after conflict.");
    }

    private static T RetryWrite<T>(Func<T> operation)
    {
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                return operation();
            }
            catch (SqliteException exception) when (
                attempt < MaxWriteAttempts &&
                (exception.SqliteErrorCode == SqliteBusy || exception.SqliteErrorCode == SqliteLocked))
            {
                Thread.Sleep(25 * attempt);
            }
        }
    }

    private static void BindDecision(SqliteCommand command, DecisionAuditRecord record)
    {
        command.Parameters.AddWithValue("$requestId", record.RequestId);
        command.Parameters.AddWithValue("$decisionId", record.DecisionId);
        command.Parameters.AddWithValue("$profile", record.Profile);
        command.Parameters.AddWithValue("$owner", record.Owner);
        command.Parameters.AddWithValue("$requestFingerprint", (object?)record.RequestFingerprint ?? DBNull.Value);
        command.Parameters.AddWithValue("$acknowledgerId", (object?)record.AcknowledgerId ?? DBNull.Value);
        command.Parameters.AddWithValue("$outcome", record.Outcome);
        command.Parameters.AddWithValue("$clearanceState", record.ClearanceState);
        command.Parameters.AddWithValue("$evidenceId", record.EvidenceId);
        command.Parameters.AddWithValue("$summary", record.Summary);
        command.Parameters.AddWithValue("$kernelVersion", record.KernelVersion);
        command.Parameters.AddWithValue("$policyVersion", record.PolicyVersion);
    }

    private static DecisionAuditRecord? GetByRequestIdWithinTransaction(
        SqliteConnection connection,
        SqliteTransaction transaction,
        string requestId)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText =
            """
            SELECT
                request_id,
                decision_id,
                profile,
                owner,
                request_fingerprint,
                acknowledger_id,
                outcome,
                clearance_state,
                evidence_id,
                summary,
                kernel_version,
                policy_version
            FROM decisions
            WHERE request_id = $requestId
            LIMIT 1;
            """;
        command.Parameters.AddWithValue("$requestId", requestId);

        using var reader = command.ExecuteReader();
        if (!reader.Read())
        {
            return null;
        }

        var record = MapRecord(reader);
        LoadConstraints(connection, transaction, record);
        LoadTimeline(connection, transaction, record);
        return record;
    }

    private static DecisionAuditRecord? GetByDecisionIdWithinTransaction(
        SqliteConnection connection,
        SqliteTransaction transaction,
        string decisionId)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText =
            """
            SELECT
                request_id,
                decision_id,
                profile,
                owner,
                request_fingerprint,
                acknowledger_id,
                outcome,
                clearance_state,
                evidence_id,
                summary,
                kernel_version,
                policy_version
            FROM decisions
            WHERE decision_id = $decisionId
            LIMIT 1;
            """;
        command.Parameters.AddWithValue("$decisionId", decisionId);

        using var reader = command.ExecuteReader();
        if (!reader.Read())
        {
            return null;
        }

        var record = MapRecord(reader);
        LoadConstraints(connection, transaction, record);
        LoadTimeline(connection, transaction, record);
        return record;
    }

    private static DecisionAuditRecord MapRecord(SqliteDataReader reader) =>
        new()
        {
            RequestId = reader.GetString(0),
            DecisionId = reader.GetString(1),
            Profile = reader.GetString(2),
            Owner = reader.GetString(3),
            RequestFingerprint = reader.IsDBNull(4) ? null : reader.GetString(4),
            AcknowledgerId = reader.IsDBNull(5) ? null : reader.GetString(5),
            Outcome = reader.GetString(6),
            ClearanceState = reader.GetString(7),
            EvidenceId = reader.GetString(8),
            Summary = reader.GetString(9),
            KernelVersion = reader.GetString(10),
            PolicyVersion = reader.GetString(11),
        };

    private static void LoadConstraints(SqliteConnection connection, DecisionAuditRecord record) =>
        LoadConstraints(connection, null, record);

    private static void LoadConstraints(
        SqliteConnection connection,
        SqliteTransaction? transaction,
        DecisionAuditRecord record)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText =
            """
            SELECT constraint_id
            FROM decision_constraints
            WHERE decision_id = $decisionId
            ORDER BY constraint_id;
            """;
        command.Parameters.AddWithValue("$decisionId", record.DecisionId);

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            record.ConstraintsApplied.Add(reader.GetString(0));
        }
    }

    private static void LoadTimeline(SqliteConnection connection, DecisionAuditRecord record) =>
        LoadTimeline(connection, null, record);

    private static void LoadTimeline(
        SqliteConnection connection,
        SqliteTransaction? transaction,
        DecisionAuditRecord record)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText =
            """
            SELECT state, timestamp
            FROM decision_timeline
            WHERE decision_id = $decisionId
            ORDER BY timeline_id;
            """;
        command.Parameters.AddWithValue("$decisionId", record.DecisionId);

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            record.Timeline.Add(new DecisionAuditTransition(
                reader.GetString(0),
                reader.GetString(1)));
        }
    }
}
