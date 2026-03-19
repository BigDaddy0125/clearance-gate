namespace ClearanceGate.Policy;

public sealed class ItOpsDeploymentPolicyEvaluator(
    ClearanceGate.Profiles.IProfileCatalog profileCatalog) : IPolicyEvaluator
{
    public PolicyEvaluationResult Evaluate(ClearanceGate.Contracts.AuthorizationRequest request)
    {
        var profile = profileCatalog.GetRequiredProfile(request.Profile);
        var constraints = new List<string>();

        foreach (var constraint in profile.Constraints)
        {
            if (string.Equals(constraint.Kind, "required_field", StringComparison.Ordinal) &&
                string.Equals(constraint.Field, "responsibility.owner", StringComparison.Ordinal) &&
                string.IsNullOrWhiteSpace(request.Responsibility.Owner))
            {
                constraints.Add(constraint.Id);
            }

            if (string.Equals(constraint.Kind, "required_field", StringComparison.Ordinal) &&
                string.Equals(constraint.Field, "metadata.source_system", StringComparison.Ordinal) &&
                string.IsNullOrWhiteSpace(request.Metadata.SourceSystem))
            {
                constraints.Add(constraint.Id);
            }
        }

        if (constraints.Count > 0)
        {
            return new PolicyEvaluationResult(
                ClearanceGate.Kernel.ClearanceState.InfoInsufficient,
                "Required authorization inputs are missing",
                constraints);
        }

        foreach (var constraint in profile.Constraints)
        {
            if (string.Equals(constraint.Kind, "ack_required", StringComparison.Ordinal) &&
                request.RiskFlags.Contains(constraint.WhenRiskFlagPresent, StringComparer.Ordinal))
            {
                constraints.Add(constraint.Id);
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
