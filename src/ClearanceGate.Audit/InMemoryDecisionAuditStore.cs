using System.Collections.Concurrent;

namespace ClearanceGate.Audit;

public sealed class InMemoryDecisionAuditStore : IDecisionAuditStore
{
    private readonly Lock gate = new();
    private readonly ConcurrentDictionary<string, DecisionAuditRecord> recordsByDecisionId = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, string> decisionIdByRequestId = new(StringComparer.Ordinal);

    public DecisionAuditRecord? GetByRequestId(string requestId)
    {
        if (!decisionIdByRequestId.TryGetValue(requestId, out var decisionId))
        {
            return null;
        }

        return GetByDecisionId(decisionId);
    }

    public DecisionAuditRecord? GetByDecisionId(string decisionId)
    {
        recordsByDecisionId.TryGetValue(decisionId, out var record);
        return record;
    }

    public DecisionAuditRecord SaveAuthorization(DecisionAuditRecord record)
    {
        lock (gate)
        {
            if (decisionIdByRequestId.TryGetValue(record.RequestId, out var existingDecisionId) &&
                recordsByDecisionId.TryGetValue(existingDecisionId, out var existingRecord))
            {
                return existingRecord;
            }

            decisionIdByRequestId[record.RequestId] = record.DecisionId;
            recordsByDecisionId[record.DecisionId] = record;
            return record;
        }
    }

    public AcknowledgmentWriteResult SaveAcknowledgment(
        string decisionId,
        string acknowledgerId,
        string outcome,
        string state,
        string summary,
        string timestamp)
    {
        if (!recordsByDecisionId.TryGetValue(decisionId, out var record))
        {
            return new AcknowledgmentWriteResult(AcknowledgmentWriteStatus.NotFound, null);
        }

        lock (record)
        {
            if (record.ClearanceState == "AUTHORIZED" &&
                string.Equals(record.AcknowledgerId, acknowledgerId, StringComparison.Ordinal))
            {
                return new AcknowledgmentWriteResult(AcknowledgmentWriteStatus.AlreadyApplied, record);
            }

            if (record.ClearanceState != "AWAITING_ACK" ||
                !record.ConstraintsApplied.Contains("RISK_ACK_REQUIRED", StringComparer.Ordinal))
            {
                return new AcknowledgmentWriteResult(AcknowledgmentWriteStatus.InvalidState, record);
            }

            record.AcknowledgerId = acknowledgerId;
            record.Outcome = outcome;
            record.ClearanceState = state;
            record.Summary = summary;
            record.Timeline.Add(new DecisionAuditTransition(state, timestamp));

            return new AcknowledgmentWriteResult(AcknowledgmentWriteStatus.Applied, record);
        }
    }
}
