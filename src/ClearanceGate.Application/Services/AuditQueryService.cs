namespace ClearanceGate.Application.Services;

public sealed class AuditQueryService : ClearanceGate.Application.Abstractions.IAuditQueryService
{
    public Task<ClearanceGate.Contracts.AuditRecordResponse?> GetAuditAsync(
        string decisionId,
        CancellationToken cancellationToken)
    {
        var response = new ClearanceGate.Contracts.AuditRecordResponse(
            decisionId,
            new[]
            {
                new ClearanceGate.Contracts.AuditTimelineItem("RISK_FLAGGED", "2026-01-01T00:00:00Z"),
                new ClearanceGate.Contracts.AuditTimelineItem("AWAITING_ACK", "2026-01-01T00:01:00Z"),
                new ClearanceGate.Contracts.AuditTimelineItem("AUTHORIZED", "2026-01-01T00:02:00Z"),
            },
            "PROCEED",
            new ClearanceGate.Contracts.AuditResponsibility("owner@example.com", "approver@example.com"),
            new[] { "RISK_ACK_REQUIRED" },
            new ClearanceGate.Contracts.VersionInfo("0.1.0", "itops_deployment_v1"));

        return Task.FromResult<ClearanceGate.Contracts.AuditRecordResponse?>(response);
    }
}
