namespace ClearanceGate.Policy;

public sealed class ProfilePolicyProjector : IProfilePolicyProjector
{
    public ProjectedPolicyProfile Project(ClearanceGate.Profiles.ClearanceProfile profile)
    {
        var requiredFieldConstraints = profile.Constraints
            .Where(constraint => string.Equals(
                constraint.Kind,
                ClearanceGate.Profiles.ProfileConstraintKinds.RequiredField,
                StringComparison.Ordinal))
            .Select(constraint => new ProjectedRequiredFieldConstraint(
                constraint.Id,
                constraint.Field!))
            .ToArray();

        var ackConstraints = profile.Constraints
            .Where(constraint => string.Equals(
                constraint.Kind,
                ClearanceGate.Profiles.ProfileConstraintKinds.AckRequired,
                StringComparison.Ordinal))
            .Select(constraint => new ProjectedAckConstraint(
                constraint.Id,
                constraint.WhenRiskFlagPresent!))
            .ToArray();

        return new ProjectedPolicyProfile(
            profile.Profile,
            requiredFieldConstraints,
            ackConstraints);
    }
}
