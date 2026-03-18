namespace ClearanceGate.Application.Services;

public sealed class AcknowledgmentService : ClearanceGate.Application.Abstractions.IAcknowledgmentService
{
    public Task<ClearanceGate.Contracts.AcknowledgmentResponse> AcknowledgeAsync(
        ClearanceGate.Contracts.AcknowledgmentRequest request,
        CancellationToken cancellationToken)
    {
        var response = new ClearanceGate.Contracts.AcknowledgmentResponse(
            request.DecisionId,
            ClearanceGate.Kernel.AuthorizationOutcome.Proceed.ToString().ToUpperInvariant(),
            ClearanceGate.Kernel.ClearanceState.Authorized.ToString().ToUpperInvariant(),
            $"evidence:{request.DecisionId}");

        return Task.FromResult(response);
    }
}
