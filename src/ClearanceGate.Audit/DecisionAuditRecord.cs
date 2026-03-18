namespace ClearanceGate.Audit;

public sealed class DecisionAuditRecord
{
    public required string RequestId { get; init; }

    public required string DecisionId { get; init; }

    public required string Profile { get; init; }

    public required string Owner { get; init; }

    public string? AcknowledgerId { get; set; }

    public required string Outcome { get; set; }

    public required string ClearanceState { get; set; }

    public required string EvidenceId { get; init; }

    public required string Summary { get; set; }

    public required string KernelVersion { get; init; }

    public required string PolicyVersion { get; init; }

    public List<string> ConstraintsApplied { get; } = new();

    public List<DecisionAuditTransition> Timeline { get; } = new();
}

public sealed record DecisionAuditTransition(
    string State,
    string Timestamp);
