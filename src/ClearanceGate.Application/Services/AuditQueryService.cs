using Microsoft.Extensions.Logging;

namespace ClearanceGate.Application.Services;

public sealed class AuditQueryService(
    ClearanceGate.Audit.IDecisionAuditStore auditStore,
    ILogger<AuditQueryService> logger) : ClearanceGate.Application.Abstractions.IAuditQueryService
{
    private static class LogEvents
    {
        public static readonly EventId LookupReturned = new(3200, nameof(LookupReturned));
        public static readonly EventId LookupNotFound = new(3201, nameof(LookupNotFound));
    }

    public Task<ClearanceGate.Contracts.AuditRecordResponse?> GetAuditAsync(
        string decisionId,
        CancellationToken cancellationToken)
    {
        var record = auditStore.GetByDecisionId(decisionId);
        LogAuditLookup("decision", decisionId, "compact", record is not null);
        return Task.FromResult(MapAuditRecord(record));
    }

    public Task<ClearanceGate.Contracts.AuditRecordResponse?> GetAuditByRequestIdAsync(
        string requestId,
        CancellationToken cancellationToken)
    {
        var record = auditStore.GetByRequestId(requestId);
        LogAuditLookup("request", requestId, "compact", record is not null);
        return Task.FromResult(MapAuditRecord(record));
    }

    public Task<ClearanceGate.Contracts.AuditExportResponse?> ExportAuditAsync(
        string decisionId,
        CancellationToken cancellationToken)
    {
        var record = auditStore.GetByDecisionId(decisionId);
        LogAuditLookup("decision", decisionId, "export", record is not null);
        return Task.FromResult(MapAuditExport(record));
    }

    public Task<ClearanceGate.Contracts.AuditExportResponse?> ExportAuditByRequestIdAsync(
        string requestId,
        CancellationToken cancellationToken)
    {
        var record = auditStore.GetByRequestId(requestId);
        LogAuditLookup("request", requestId, "export", record is not null);
        return Task.FromResult(MapAuditExport(record));
    }

    private void LogAuditLookup(string lookupKind, string lookupValue, string viewKind, bool found)
    {
        if (found)
        {
            logger.LogInformation(
                LogEvents.LookupReturned,
                "Audit view returned record. LookupKind={LookupKind} LookupValue={LookupValue} ViewKind={ViewKind}",
                lookupKind,
                lookupValue,
                viewKind);
            return;
        }

        logger.LogWarning(
            LogEvents.LookupNotFound,
            "Audit view did not find record. LookupKind={LookupKind} LookupValue={LookupValue} ViewKind={ViewKind}",
            lookupKind,
            lookupValue,
            viewKind);
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
