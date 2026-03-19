namespace ClearanceGate.Contracts;

public sealed record AuditRecordResponse(
    string DecisionId,
    string EvidenceId,
    IReadOnlyList<AuditTimelineItem> AuthorizationTimeline,
    string Outcome,
    AuditResponsibility Responsibility,
    IReadOnlyList<string> ConstraintsApplied,
    VersionInfo Version);

public sealed record AuditTimelineItem(
    string State,
    string Timestamp);

public sealed record AuditResponsibility(
    string Owner,
    string Acknowledger);
