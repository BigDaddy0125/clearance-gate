namespace ClearanceGate.Application.Services;

public sealed class AuditQueryService(
    ClearanceGate.Audit.IDecisionAuditStore auditStore) : ClearanceGate.Application.Abstractions.IAuditQueryService
{
    public Task<ClearanceGate.Contracts.AuditRecordResponse?> GetAuditAsync(
        string decisionId,
        CancellationToken cancellationToken)
    {
        return Task.FromResult(MapAuditRecord(auditStore.GetByDecisionId(decisionId)));
    }

    public Task<ClearanceGate.Contracts.AuditRecordResponse?> GetAuditByRequestIdAsync(
        string requestId,
        CancellationToken cancellationToken)
    {
        return Task.FromResult(MapAuditRecord(auditStore.GetByRequestId(requestId)));
    }

    public Task<ClearanceGate.Contracts.AuditExportResponse?> ExportAuditAsync(
        string decisionId,
        CancellationToken cancellationToken)
    {
        return Task.FromResult(MapAuditExport(auditStore.GetByDecisionId(decisionId)));
    }

    public Task<ClearanceGate.Contracts.AuditExportResponse?> ExportAuditByRequestIdAsync(
        string requestId,
        CancellationToken cancellationToken)
    {
        return Task.FromResult(MapAuditExport(auditStore.GetByRequestId(requestId)));
    }

    private static ClearanceGate.Contracts.AuditRecordResponse? MapAuditRecord(ClearanceGate.Audit.DecisionAuditRecord? record)
    {
        if (record is null)
        {
            return null;
        }

        return new ClearanceGate.Contracts.AuditRecordResponse(
            record.DecisionId,
            record.EvidenceId,
            MapTimeline(record),
            record.Outcome,
            new ClearanceGate.Contracts.AuditResponsibility(record.Owner, record.AcknowledgerId ?? string.Empty),
            record.ConstraintsApplied.ToArray(),
            new ClearanceGate.Contracts.VersionInfo(record.KernelVersion, record.PolicyVersion));
    }

    private static ClearanceGate.Contracts.AuditExportResponse? MapAuditExport(ClearanceGate.Audit.DecisionAuditRecord? record)
    {
        if (record is null)
        {
            return null;
        }

        return new ClearanceGate.Contracts.AuditExportResponse(
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
    }

    private static ClearanceGate.Contracts.AuditTimelineItem[] MapTimeline(ClearanceGate.Audit.DecisionAuditRecord record) =>
        record.Timeline
            .Select(item => new ClearanceGate.Contracts.AuditTimelineItem(item.State, item.Timestamp))
            .ToArray();
}
