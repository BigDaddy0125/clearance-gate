namespace ClearanceGate.Contracts;

public sealed record AuthorizationRequest(
    string RequestId,
    string DecisionId,
    string Profile,
    ActionDescriptor Action,
    DecisionContext Context,
    IReadOnlyList<string> RiskFlags,
    ResponsibilityDescriptor Responsibility,
    RequestMetadata Metadata);

public sealed record ActionDescriptor(
    string Type,
    string Description);

public sealed record DecisionContext(
    IReadOnlyDictionary<string, string> Attributes);

public sealed record ResponsibilityDescriptor(
    string Owner,
    string Role);

public sealed record RequestMetadata(
    string SourceSystem,
    string Timestamp);

public sealed record AuthorizationResponse(
    string DecisionId,
    string Outcome,
    string ClearanceState,
    string EvidenceId,
    AuthorizationReason Reason,
    VersionInfo Version);

public sealed record AuthorizationReason(
    string Summary,
    IReadOnlyList<string> ConstraintsTriggered);

public sealed record VersionInfo(
    string Kernel,
    string Policy);
