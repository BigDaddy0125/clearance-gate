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
            record.EvidenceId,
            MapTimeline(record),
            record.Outcome,
            new ClearanceGate.Contracts.AuditResponsibility(record.Owner, record.AcknowledgerId ?? string.Empty),
            record.ConstraintsApplied.ToArray(),
            new ClearanceGate.Contracts.VersionInfo(record.KernelVersion, record.PolicyVersion));

        return Task.FromResult<ClearanceGate.Contracts.AuditRecordResponse?>(response);
    }

    public Task<ClearanceGate.Contracts.AuditExportResponse?> ExportAuditAsync(
        string decisionId,
        CancellationToken cancellationToken)
    {
        var record = auditStore.GetByDecisionId(decisionId);
        if (record is null)
        {
            return Task.FromResult<ClearanceGate.Contracts.AuditExportResponse?>(null);
        }

        var response = new ClearanceGate.Contracts.AuditExportResponse(
            record.DecisionId,
            record.RequestId,
            record.Profile,
            record.EvidenceId,
            record.Outcome,
            record.ClearanceState,
            record.Summary,
            new ClearanceGate.Contracts.AuditResponsibility(record.Owner, record.AcknowledgerId ?? string.Empty),
            record.ConstraintsApplied.ToArray(),
            MapTimeline(record),
            new ClearanceGate.Contracts.VersionInfo(record.KernelVersion, record.PolicyVersion));

        return Task.FromResult<ClearanceGate.Contracts.AuditExportResponse?>(response);
    }

    private static ClearanceGate.Contracts.AuditTimelineItem[] MapTimeline(ClearanceGate.Audit.DecisionAuditRecord record) =>
        record.Timeline
            .Select(item => new ClearanceGate.Contracts.AuditTimelineItem(item.State, item.Timestamp))
            .ToArray();
}
