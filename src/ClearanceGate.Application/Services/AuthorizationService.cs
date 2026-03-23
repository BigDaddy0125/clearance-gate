using ClearanceGate.Kernel;
using Microsoft.Extensions.Logging;

namespace ClearanceGate.Application.Services;

public sealed class AuthorizationService(
    ClearanceGate.Policy.IPolicyEvaluator policyEvaluator,
    ClearanceGate.Audit.IDecisionAuditStore auditStore,
    ClearanceGate.Profiles.IProfileCatalog profileCatalog,
    ILogger<AuthorizationService> logger) : ClearanceGate.Application.Abstractions.IAuthorizationService
{
    public Task<ClearanceGate.Contracts.AuthorizationResponse> AuthorizeAsync(
        ClearanceGate.Contracts.AuthorizationRequest request,
        CancellationToken cancellationToken)
    {
        var existingRecord = auditStore.GetByRequestId(request.RequestId);
        if (existingRecord is not null)
        {
            logger.LogInformation(
                "Authorization request replayed existing record. RequestId={RequestId} DecisionId={DecisionId} Outcome={Outcome} ClearanceState={ClearanceState}",
                request.RequestId,
                existingRecord.DecisionId,
                existingRecord.Outcome,
                existingRecord.ClearanceState);
            return Task.FromResult(ToResponse(existingRecord));
        }

        var profile = profileCatalog.GetRequiredProfile(request.Profile);
        EnsureRoleAllowed(
            profile,
            request.Responsibility.Role,
            ClearanceGate.Profiles.KernelResponsibilityRoles.DecisionOwner,
            "authorization");

        var evaluation = policyEvaluator.Evaluate(request);

        var decision = new ClearanceGate.Kernel.DecisionEvaluation(
            request.DecisionId,
            evaluation.State);

        var outcome = ClearanceGate.Kernel.ClearanceKernel.MapOutcome(decision.State);

        var record = new ClearanceGate.Audit.DecisionAuditRecord
        {
            RequestId = request.RequestId,
            DecisionId = request.DecisionId,
            Profile = request.Profile,
            Owner = request.Responsibility.Owner,
            Outcome = outcome.ToWireName(),
            ClearanceState = decision.State.ToWireName(),
            EvidenceId = $"evidence:{request.DecisionId}",
            Summary = evaluation.Summary,
            KernelVersion = "0.1.0",
            PolicyVersion = request.Profile,
        };

        record.ConstraintsApplied.AddRange(evaluation.ConstraintsTriggered);
        record.Timeline.Add(new ClearanceGate.Audit.DecisionAuditTransition(
            decision.State.ToWireName(),
            request.Metadata.Timestamp));

        var savedRecord = auditStore.SaveAuthorization(record);
        if (savedRecord.DecisionId == request.DecisionId && savedRecord.Timeline.Count == 0)
        {
            throw new InvalidOperationException($"Evidence for decision '{request.DecisionId}' was not stored.");
        }

        logger.LogInformation(
            "Authorization decision recorded. RequestId={RequestId} DecisionId={DecisionId} Profile={Profile} Outcome={Outcome} ClearanceState={ClearanceState} EvidenceId={EvidenceId} ConstraintCount={ConstraintCount}",
            savedRecord.RequestId,
            savedRecord.DecisionId,
            savedRecord.Profile,
            savedRecord.Outcome,
            savedRecord.ClearanceState,
            savedRecord.EvidenceId,
            savedRecord.ConstraintsApplied.Count);

        return Task.FromResult(ToResponse(savedRecord));
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
                "Authorization boundary rejected profile role mapping. Profile={Profile} RequiredRole={RequiredRole} Operation={Operation}",
                profile.Profile,
                requiredRole,
                operation);
            throw new InvalidOperationException(
                $"Profile '{profile.Profile}' does not permit role '{requiredRole}' for {operation}.");
        }

        if (!string.Equals(actualRole, requiredRole, StringComparison.Ordinal))
        {
            logger.LogWarning(
                "Authorization boundary rejected caller role. Profile={Profile} ActualRole={ActualRole} RequiredRole={RequiredRole} Operation={Operation}",
                profile.Profile,
                actualRole,
                requiredRole,
                operation);
            throw new ArgumentException(
                $"Role '{actualRole}' is not permitted for {operation}; expected '{requiredRole}'.");
        }
    }

    private static ClearanceGate.Contracts.AuthorizationResponse ToResponse(ClearanceGate.Audit.DecisionAuditRecord record) =>
        new(
            record.DecisionId,
            record.Outcome,
            record.ClearanceState,
            record.EvidenceId,
            new ClearanceGate.Contracts.AuthorizationReason(
                record.Summary,
                record.ConstraintsApplied),
            new ClearanceGate.Contracts.VersionInfo(
                record.KernelVersion,
                record.PolicyVersion));
}
