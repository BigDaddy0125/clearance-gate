namespace ClearanceGate.Application.Abstractions;

public interface IAcknowledgmentService
{
    Task<ClearanceGate.Contracts.AcknowledgmentResponse> AcknowledgeAsync(
        ClearanceGate.Contracts.AcknowledgmentRequest request,
        CancellationToken cancellationToken);
}
