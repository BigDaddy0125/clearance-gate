using ClearanceGate.Kernel;

namespace ClearanceGate.Application.Services;

public sealed class AcknowledgmentService(
    ClearanceGate.Audit.IDecisionAuditStore auditStore) : ClearanceGate.Application.Abstractions.IAcknowledgmentService
{
    public Task<ClearanceGate.Contracts.AcknowledgmentResponse> AcknowledgeAsync(
        ClearanceGate.Contracts.AcknowledgmentRequest request,
        CancellationToken cancellationToken)
    {
        var writeResult = auditStore.SaveAcknowledgment(
            request.DecisionId,
            request.Acknowledger.Id,
            ClearanceGate.Kernel.AuthorizationOutcome.Proceed.ToWireName(),
            ClearanceGate.Kernel.ClearanceState.Authorized.ToWireName(),
            "Authorization acknowledged by designated authority",
            request.Acknowledgment.Timestamp);

        if (writeResult.Status == ClearanceGate.Audit.AcknowledgmentWriteStatus.NotFound)
        {
            throw new KeyNotFoundException($"Decision '{request.DecisionId}' was not found.");
        }

        if (writeResult.Status == ClearanceGate.Audit.AcknowledgmentWriteStatus.InvalidState)
        {
            throw new InvalidOperationException($"Decision '{request.DecisionId}' is not eligible for acknowledgment.");
        }

        var updatedRecord = writeResult.Record!;

        var response = new ClearanceGate.Contracts.AcknowledgmentResponse(
            updatedRecord.DecisionId,
            updatedRecord.Outcome,
            updatedRecord.ClearanceState,
            updatedRecord.EvidenceId);

        return Task.FromResult(response);
    }
}
