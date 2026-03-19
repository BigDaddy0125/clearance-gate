namespace ClearanceGate.Policy;

public sealed class ItOpsDeploymentPolicyEvaluator(
    ClearanceGate.Profiles.IProfileCatalog profileCatalog,
    IProfilePolicyProjector projector) : IPolicyEvaluator
{
    public PolicyEvaluationResult Evaluate(ClearanceGate.Contracts.AuthorizationRequest request)
    {
        var profile = profileCatalog.GetRequiredProfile(request.Profile);
        var projectedProfile = projector.Project(profile);
        var constraints = new List<string>();

        foreach (var constraint in projectedProfile.RequiredFieldConstraints)
        {
            if (string.Equals(
                    constraint.Field,
                    ClearanceGate.Profiles.ProfileFieldPaths.ResponsibilityOwner,
                    StringComparison.Ordinal) &&
                string.IsNullOrWhiteSpace(request.Responsibility.Owner))
            {
                constraints.Add(constraint.ConstraintId);
            }

            if (string.Equals(
                    constraint.Field,
                    ClearanceGate.Profiles.ProfileFieldPaths.MetadataSourceSystem,
                    StringComparison.Ordinal) &&
                string.IsNullOrWhiteSpace(request.Metadata.SourceSystem))
            {
                constraints.Add(constraint.ConstraintId);
            }
        }

        if (constraints.Count > 0)
        {
            return new PolicyEvaluationResult(
                ClearanceGate.Kernel.ClearanceState.InfoInsufficient,
                "Required authorization inputs are missing",
                constraints);
        }

        foreach (var constraint in projectedProfile.AckConstraints)
        {
            if (request.RiskFlags.Contains(constraint.RiskFlag, StringComparer.Ordinal))
            {
                constraints.Add(constraint.ConstraintId);
            }
        }

        if (constraints.Count > 0)
        {
            return new PolicyEvaluationResult(
                ClearanceGate.Kernel.ClearanceState.AwaitingAck,
                "Explicit acknowledgment required",
                constraints);
        }

        return new PolicyEvaluationResult(
            ClearanceGate.Kernel.ClearanceState.Authorized,
            "Authorized under current skeleton policy",
            constraints);
    }
}
