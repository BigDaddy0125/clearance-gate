using ClearanceGate.Kernel;

namespace ClearanceGate.Application.Services;

public sealed class AuthorizationService(
    ClearanceGate.Policy.IPolicyEvaluator policyEvaluator,
    ClearanceGate.Audit.IDecisionAuditStore auditStore) : ClearanceGate.Application.Abstractions.IAuthorizationService
{
    public Task<ClearanceGate.Contracts.AuthorizationResponse> AuthorizeAsync(
        ClearanceGate.Contracts.AuthorizationRequest request,
        CancellationToken cancellationToken)
    {
        var existingRecord = auditStore.GetByRequestId(request.RequestId);
        if (existingRecord is not null)
        {
            return Task.FromResult(ToResponse(existingRecord));
        }

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

        return Task.FromResult(ToResponse(savedRecord));
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
