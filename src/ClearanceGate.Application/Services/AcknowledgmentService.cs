using ClearanceGate.Kernel;

namespace ClearanceGate.Application.Services;

public sealed class AcknowledgmentService(
    ClearanceGate.Audit.IDecisionAuditStore auditStore,
    ClearanceGate.Profiles.IProfileCatalog profileCatalog) : ClearanceGate.Application.Abstractions.IAcknowledgmentService
{
    public Task<ClearanceGate.Contracts.AcknowledgmentResponse> AcknowledgeAsync(
        ClearanceGate.Contracts.AcknowledgmentRequest request,
        CancellationToken cancellationToken)
    {
        var decision = auditStore.GetByDecisionId(request.DecisionId);
        if (decision is null)
        {
            throw new KeyNotFoundException($"Decision '{request.DecisionId}' was not found.");
        }

        var profile = profileCatalog.GetRequiredProfile(decision.Profile);
        EnsureRoleAllowed(
            profile,
            request.Acknowledger.Role,
            ClearanceGate.Profiles.KernelResponsibilityRoles.AcknowledgingAuthority,
            "acknowledgment");

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

    private static void EnsureRoleAllowed(
        ClearanceGate.Profiles.ClearanceProfile profile,
        string actualRole,
        string requiredRole,
        string operation)
    {
        if (!profile.ResponsibilityRoles.Contains(requiredRole, StringComparer.Ordinal))
        {
            throw new InvalidOperationException(
                $"Profile '{profile.Profile}' does not permit role '{requiredRole}' for {operation}.");
        }

        if (!string.Equals(actualRole, requiredRole, StringComparison.Ordinal))
        {
            throw new ArgumentException(
                $"Role '{actualRole}' is not permitted for {operation}; expected '{requiredRole}'.");
        }
    }
}
