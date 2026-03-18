namespace ClearanceGate.Application.Abstractions;

public interface IAuditQueryService
{
    Task<ClearanceGate.Contracts.AuditRecordResponse?> GetAuditAsync(
        string decisionId,
        CancellationToken cancellationToken);
}
