namespace ClearanceGate.Application.Services;

public sealed class AuditQueryService(
    ClearanceGate.Audit.IDecisionAuditStore auditStore) : ClearanceGate.Application.Abstractions.IAuditQueryService
{
    public Task<ClearanceGate.Contracts.AuditRecordResponse?> GetAuditAsync(
        string decisionId,
        CancellationToken cancellationToken)
    {
        var record = auditStore.GetByDecisionId(decisionId);
        if (record is null)
        {
            return Task.FromResult<ClearanceGate.Contracts.AuditRecordResponse?>(null);
        }

        var response = new ClearanceGate.Contracts.AuditRecordResponse(
            record.DecisionId,
            record.Timeline
                .Select(item => new ClearanceGate.Contracts.AuditTimelineItem(item.State, item.Timestamp))
                .ToArray(),
            record.Outcome,
            new ClearanceGate.Contracts.AuditResponsibility(record.Owner, record.AcknowledgerId ?? string.Empty),
            record.ConstraintsApplied.ToArray(),
            new ClearanceGate.Contracts.VersionInfo(record.KernelVersion, record.PolicyVersion));

        return Task.FromResult<ClearanceGate.Contracts.AuditRecordResponse?>(response);
    }
}
