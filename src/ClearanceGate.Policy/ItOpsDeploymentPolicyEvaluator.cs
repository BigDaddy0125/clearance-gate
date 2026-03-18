namespace ClearanceGate.Policy;

public sealed class ItOpsDeploymentPolicyEvaluator : IPolicyEvaluator
{
    public PolicyEvaluationResult Evaluate(ClearanceGate.Contracts.AuthorizationRequest request)
    {
        var constraints = new List<string>();

        if (string.IsNullOrWhiteSpace(request.Responsibility.Owner))
        {
            constraints.Add("OWNER_REQUIRED");
        }

        if (string.IsNullOrWhiteSpace(request.Metadata.SourceSystem))
        {
            constraints.Add("SOURCE_REQUIRED");
        }

        if (constraints.Count > 0)
        {
            return new PolicyEvaluationResult(
                ClearanceGate.Kernel.ClearanceState.InfoInsufficient,
                "Required authorization inputs are missing",
                constraints);
        }

        if (request.RiskFlags.Count > 0)
        {
            constraints.Add("RISK_ACK_REQUIRED");

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
