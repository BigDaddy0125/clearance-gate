namespace ClearanceGate.Audit;

public enum AcknowledgmentWriteStatus
{
    Applied = 0,
    AlreadyApplied = 1,
    NotFound = 2,
    InvalidState = 3,
}

public sealed record AcknowledgmentWriteResult(
    AcknowledgmentWriteStatus Status,
    DecisionAuditRecord? Record);
