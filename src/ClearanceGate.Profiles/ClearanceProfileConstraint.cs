namespace ClearanceGate.Profiles;

public sealed record ClearanceProfileConstraint(
    string Id,
    string Kind,
    string? Field,
    string? WhenRiskFlagPresent);
