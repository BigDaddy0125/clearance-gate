namespace ClearanceGate.Profiles;

public sealed record ClearanceProfile(
    string Profile,
    string Description,
    IReadOnlyList<string> ResponsibilityRoles,
    IReadOnlyList<ClearanceProfileConstraint> Constraints);
