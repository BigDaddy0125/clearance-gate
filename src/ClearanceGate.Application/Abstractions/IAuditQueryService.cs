namespace ClearanceGate.Application.Abstractions;

public interface IAuditQueryService
{
    Task<ClearanceGate.Contracts.AuditRecordResponse?> GetAuditAsync(
        string decisionId,
        CancellationToken cancellationToken);

    Task<ClearanceGate.Contracts.AuditRecordResponse?> GetAuditByRequestIdAsync(
        string requestId,
        CancellationToken cancellationToken);

    Task<ClearanceGate.Contracts.AuditExportResponse?> ExportAuditAsync(
        string decisionId,
        CancellationToken cancellationToken);

    Task<ClearanceGate.Contracts.AuditExportResponse?> ExportAuditByRequestIdAsync(
        string requestId,
        CancellationToken cancellationToken);
}
