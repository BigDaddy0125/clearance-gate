using ClearanceGate.Kernel;
using Microsoft.Extensions.Logging;

namespace ClearanceGate.Application.Services;

public sealed class AcknowledgmentService(
    ClearanceGate.Audit.IDecisionAuditStore auditStore,
    ClearanceGate.Profiles.IProfileCatalog profileCatalog,
    ILogger<AcknowledgmentService> logger) : ClearanceGate.Application.Abstractions.IAcknowledgmentService
{
    public Task<ClearanceGate.Contracts.AcknowledgmentResponse> AcknowledgeAsync(
        ClearanceGate.Contracts.AcknowledgmentRequest request,
        CancellationToken cancellationToken)
    {
        var decision = auditStore.GetByDecisionId(request.DecisionId);
        if (decision is null)
        {
            logger.LogWarning(
                "Acknowledgment rejected because decision was not found. DecisionId={DecisionId}",
                request.DecisionId);
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
            logger.LogWarning(
                "Acknowledgment rejected because decision disappeared before write. DecisionId={DecisionId}",
                request.DecisionId);
            throw new KeyNotFoundException($"Decision '{request.DecisionId}' was not found.");
        }

        if (writeResult.Status == ClearanceGate.Audit.AcknowledgmentWriteStatus.InvalidState)
        {
            logger.LogWarning(
                "Acknowledgment rejected because decision was not eligible. DecisionId={DecisionId} ClearanceState={ClearanceState}",
                request.DecisionId,
                decision.ClearanceState);
            throw new InvalidOperationException($"Decision '{request.DecisionId}' is not eligible for acknowledgment.");
        }

        var updatedRecord = writeResult.Record!;

        logger.LogInformation(
            "Acknowledgment recorded. DecisionId={DecisionId} AcknowledgerId={AcknowledgerId} Outcome={Outcome} ClearanceState={ClearanceState} EvidenceId={EvidenceId} Status={Status}",
            updatedRecord.DecisionId,
            updatedRecord.AcknowledgerId,
            updatedRecord.Outcome,
            updatedRecord.ClearanceState,
            updatedRecord.EvidenceId,
            writeResult.Status);

        var response = new ClearanceGate.Contracts.AcknowledgmentResponse(
            updatedRecord.DecisionId,
            updatedRecord.Outcome,
            updatedRecord.ClearanceState,
            updatedRecord.EvidenceId);

        return Task.FromResult(response);
    }

    private void EnsureRoleAllowed(
        ClearanceGate.Profiles.ClearanceProfile profile,
        string actualRole,
        string requiredRole,
        string operation)
    {
        if (!profile.ResponsibilityRoles.Contains(requiredRole, StringComparer.Ordinal))
        {
            logger.LogWarning(
                "Acknowledgment boundary rejected profile role mapping. Profile={Profile} RequiredRole={RequiredRole} Operation={Operation}",
                profile.Profile,
                requiredRole,
                operation);
            throw new InvalidOperationException(
                $"Profile '{profile.Profile}' does not permit role '{requiredRole}' for {operation}.");
        }

        if (!string.Equals(actualRole, requiredRole, StringComparison.Ordinal))
        {
            logger.LogWarning(
                "Acknowledgment boundary rejected caller role. Profile={Profile} ActualRole={ActualRole} RequiredRole={RequiredRole} Operation={Operation}",
                profile.Profile,
                actualRole,
                requiredRole,
                operation);
            throw new ArgumentException(
                $"Role '{actualRole}' is not permitted for {operation}; expected '{requiredRole}'.");
        }
    }
}
