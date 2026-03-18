namespace ClearanceGate.Audit;

public interface IDecisionAuditStore
{
    DecisionAuditRecord? GetByRequestId(string requestId);

    DecisionAuditRecord? GetByDecisionId(string decisionId);

    DecisionAuditRecord SaveAuthorization(DecisionAuditRecord record);

    AcknowledgmentWriteResult SaveAcknowledgment(
        string decisionId,
        string acknowledgerId,
        string outcome,
        string state,
        string summary,
        string timestamp);
}
