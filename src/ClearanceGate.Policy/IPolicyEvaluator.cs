namespace ClearanceGate.Policy;

public interface IPolicyEvaluator
{
    PolicyEvaluationResult Evaluate(ClearanceGate.Contracts.AuthorizationRequest request);
}
