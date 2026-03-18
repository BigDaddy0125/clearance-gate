namespace ClearanceGate.Contracts;

public sealed record AcknowledgmentRequest(
    string DecisionId,
    Acknowledger Acknowledger,
    AcknowledgmentPayload Acknowledgment);

public sealed record Acknowledger(
    string Id,
    string Role);

public sealed record AcknowledgmentPayload(
    string Type,
    string Timestamp);

public sealed record AcknowledgmentResponse(
    string DecisionId,
    string Outcome,
    string ClearanceState,
    string EvidenceId);
