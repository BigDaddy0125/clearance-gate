namespace ClearanceGate.Policy;

public sealed record ProjectedPolicyProfile(
    string ProfileName,
    IReadOnlyList<ProjectedRequiredFieldConstraint> RequiredFieldConstraints,
    IReadOnlyList<ProjectedAckConstraint> AckConstraints);
