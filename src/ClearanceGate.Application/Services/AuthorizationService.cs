namespace ClearanceGate.Application.Services;

public sealed class AuthorizationService : ClearanceGate.Application.Abstractions.IAuthorizationService
{
    public Task<ClearanceGate.Contracts.AuthorizationResponse> AuthorizeAsync(
        ClearanceGate.Contracts.AuthorizationRequest request,
        CancellationToken cancellationToken)
    {
        var decision = new ClearanceGate.Kernel.DecisionEvaluation(
            request.DecisionId,
            request.RiskFlags.Count > 0 ? ClearanceGate.Kernel.ClearanceState.RiskFlagged : ClearanceGate.Kernel.ClearanceState.Authorized);

        var outcome = ClearanceGate.Kernel.ClearanceKernel.MapOutcome(decision.State);

        var response = new ClearanceGate.Contracts.AuthorizationResponse(
            request.DecisionId,
            outcome.ToString().ToUpperInvariant(),
            decision.State.ToString().ToUpperInvariant(),
            $"evidence:{request.DecisionId}",
            new ClearanceGate.Contracts.AuthorizationReason(
                outcome == ClearanceGate.Kernel.AuthorizationOutcome.RequireAck
                    ? "Explicit acknowledgment required"
                    : "Initial skeleton response",
                request.RiskFlags),
            new ClearanceGate.Contracts.VersionInfo("0.1.0", request.Profile));

        return Task.FromResult(response);
    }
}
