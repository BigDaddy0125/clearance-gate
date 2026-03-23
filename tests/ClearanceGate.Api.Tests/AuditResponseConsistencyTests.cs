using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace ClearanceGate.Api.Tests;

public sealed class AuditResponseConsistencyTests
{
    [Fact]
    public async Task CompactAndExportViews_AgreeAcrossDecisionAndRequestEndpoints_BeforeAcknowledgment()
    {
        using var harness = CreateHarness();
        using var factory = new ClearanceGateApiFactory(harness.DatabasePath);
        using var client = factory.CreateClient();
        var request = BuildAuthorizationRequest("req-consistency-1", "dec-consistency-1", new[] { "HIGH_IMPACT" }, "alice", "change-control");

        var authorizeResponse = await client.PostAsJsonAsync("/authorize", request);
        var compactByDecision = await client.GetFromJsonAsync<ClearanceGate.Contracts.AuditRecordResponse>("/audit/dec-consistency-1");
        var compactByRequest = await client.GetFromJsonAsync<ClearanceGate.Contracts.AuditRecordResponse>("/audit/request/req-consistency-1");
        var exportByDecision = await client.GetFromJsonAsync<ClearanceGate.Contracts.AuditExportResponse>("/audit/dec-consistency-1/export");
        var exportByRequest = await client.GetFromJsonAsync<ClearanceGate.Contracts.AuditExportResponse>("/audit/request/req-consistency-1/export");

        Assert.Equal(HttpStatusCode.OK, authorizeResponse.StatusCode);
        Assert.NotNull(compactByDecision);
        Assert.NotNull(compactByRequest);
        Assert.NotNull(exportByDecision);
        Assert.NotNull(exportByRequest);

        AssertCompactRecordsEqual(compactByDecision, compactByRequest);
        AssertExportRecordsEqual(exportByDecision, exportByRequest);
        AssertCompactMatchesExport(compactByDecision, exportByDecision);
    }

    [Fact]
    public async Task CompactAndExportViews_AgreeAcrossDecisionAndRequestEndpoints_AfterAcknowledgment()
    {
        using var harness = CreateHarness();
        using var factory = new ClearanceGateApiFactory(harness.DatabasePath);
        using var client = factory.CreateClient();
        var request = BuildAuthorizationRequest("req-consistency-2", "dec-consistency-2", new[] { "HIGH_IMPACT" }, "alice", "change-control");

        var authorizeResponse = await client.PostAsJsonAsync("/authorize", request);
        var acknowledgeResponse = await client.PostAsJsonAsync("/acknowledge", new ClearanceGate.Contracts.AcknowledgmentRequest(
            "dec-consistency-2",
            new ClearanceGate.Contracts.Acknowledger("alice", "acknowledging_authority"),
            new ClearanceGate.Contracts.AcknowledgmentPayload("risk_acceptance", "2026-03-18T10:05:00Z")));
        var compactByDecision = await client.GetFromJsonAsync<ClearanceGate.Contracts.AuditRecordResponse>("/audit/dec-consistency-2");
        var compactByRequest = await client.GetFromJsonAsync<ClearanceGate.Contracts.AuditRecordResponse>("/audit/request/req-consistency-2");
        var exportByDecision = await client.GetFromJsonAsync<ClearanceGate.Contracts.AuditExportResponse>("/audit/dec-consistency-2/export");
        var exportByRequest = await client.GetFromJsonAsync<ClearanceGate.Contracts.AuditExportResponse>("/audit/request/req-consistency-2/export");

        Assert.Equal(HttpStatusCode.OK, authorizeResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, acknowledgeResponse.StatusCode);
        Assert.NotNull(compactByDecision);
        Assert.NotNull(compactByRequest);
        Assert.NotNull(exportByDecision);
        Assert.NotNull(exportByRequest);

        AssertCompactRecordsEqual(compactByDecision, compactByRequest);
        AssertExportRecordsEqual(exportByDecision, exportByRequest);
        AssertCompactMatchesExport(compactByDecision, exportByDecision);
        Assert.Equal(new[] { "AWAITING_ACK", "AUTHORIZED" }, exportByDecision.AuthorizationTimeline.Select(item => item.State));
    }

    [Fact]
    public async Task AuditViews_ReturnStableOrderingAcrossRepeatedReads()
    {
        using var harness = CreateHarness();
        using var factory = new ClearanceGateApiFactory(harness.DatabasePath);
        using var client = factory.CreateClient();
        var request = BuildAuthorizationRequest("req-consistency-3", "dec-consistency-3", Array.Empty<string>(), "alice", "change-control");

        var authorizeResponse = await client.PostAsJsonAsync("/authorize", request);
        var firstCompact = await client.GetFromJsonAsync<ClearanceGate.Contracts.AuditRecordResponse>("/audit/dec-consistency-3");
        var secondCompact = await client.GetFromJsonAsync<ClearanceGate.Contracts.AuditRecordResponse>("/audit/dec-consistency-3");
        var firstExport = await client.GetFromJsonAsync<ClearanceGate.Contracts.AuditExportResponse>("/audit/dec-consistency-3/export");
        var secondExport = await client.GetFromJsonAsync<ClearanceGate.Contracts.AuditExportResponse>("/audit/dec-consistency-3/export");

        Assert.Equal(HttpStatusCode.OK, authorizeResponse.StatusCode);
        Assert.NotNull(firstCompact);
        Assert.NotNull(secondCompact);
        Assert.NotNull(firstExport);
        Assert.NotNull(secondExport);

        AssertCompactRecordsEqual(firstCompact, secondCompact);
        AssertExportRecordsEqual(firstExport, secondExport);
    }

    private static void AssertCompactRecordsEqual(
        ClearanceGate.Contracts.AuditRecordResponse expected,
        ClearanceGate.Contracts.AuditRecordResponse actual)
    {
        Assert.Equal(expected.DecisionId, actual.DecisionId);
        Assert.Equal(expected.EvidenceId, actual.EvidenceId);
        Assert.Equal(expected.Outcome, actual.Outcome);
        Assert.Equal(expected.Responsibility, actual.Responsibility);
        Assert.Equal(expected.ConstraintsApplied, actual.ConstraintsApplied);
        Assert.Equal(expected.Version, actual.Version);
        Assert.Equal(expected.AuthorizationTimeline, actual.AuthorizationTimeline);
    }

    private static void AssertExportRecordsEqual(
        ClearanceGate.Contracts.AuditExportResponse expected,
        ClearanceGate.Contracts.AuditExportResponse actual)
    {
        Assert.Equal(expected.DecisionId, actual.DecisionId);
        Assert.Equal(expected.RequestId, actual.RequestId);
        Assert.Equal(expected.Profile, actual.Profile);
        Assert.Equal(expected.EvidenceId, actual.EvidenceId);
        Assert.Equal(expected.Outcome, actual.Outcome);
        Assert.Equal(expected.ClearanceState, actual.ClearanceState);
        Assert.Equal(expected.Summary, actual.Summary);
        Assert.Equal(expected.Responsibility, actual.Responsibility);
        Assert.Equal(expected.ConstraintsApplied, actual.ConstraintsApplied);
        Assert.Equal(expected.AuthorizationTimeline, actual.AuthorizationTimeline);
        Assert.Equal(expected.Version, actual.Version);
    }

    private static void AssertCompactMatchesExport(
        ClearanceGate.Contracts.AuditRecordResponse compact,
        ClearanceGate.Contracts.AuditExportResponse export)
    {
        Assert.Equal(compact.DecisionId, export.DecisionId);
        Assert.Equal(compact.EvidenceId, export.EvidenceId);
        Assert.Equal(compact.Outcome, export.Outcome);
        Assert.Equal(compact.Responsibility, export.Responsibility);
        Assert.Equal(compact.ConstraintsApplied, export.ConstraintsApplied);
        Assert.Equal(compact.AuthorizationTimeline, export.AuthorizationTimeline);
        Assert.Equal(compact.Version, export.Version);
    }

    private static ClearanceGate.Contracts.AuthorizationRequest BuildAuthorizationRequest(
        string requestId,
        string decisionId,
        IReadOnlyList<string> riskFlags,
        string owner,
        string sourceSystem) =>
        new(
            requestId,
            decisionId,
            "itops_deployment_v1",
            new ClearanceGate.Contracts.ActionDescriptor("deploy", "Promote release 2026.03.18"),
            new ClearanceGate.Contracts.DecisionContext(new Dictionary<string, string>
            {
                ["changeWindow"] = "business-hours",
            }),
            riskFlags,
            new ClearanceGate.Contracts.ResponsibilityDescriptor(owner, "decision_owner"),
            new ClearanceGate.Contracts.RequestMetadata(sourceSystem, "2026-03-18T10:00:00Z"));

    private static TemporaryDatabaseHarness CreateHarness()
    {
        var path = Path.Combine(Path.GetTempPath(), $"clearancegate-consistency-tests-{Guid.NewGuid():N}.db");
        return new TemporaryDatabaseHarness(path);
    }

    private sealed class TemporaryDatabaseHarness : IDisposable
    {
        public TemporaryDatabaseHarness(string databasePath)
        {
            DatabasePath = databasePath;
        }

        public string DatabasePath { get; }

        public void Dispose()
        {
            TryDelete(DatabasePath);
            TryDelete($"{DatabasePath}-shm");
            TryDelete($"{DatabasePath}-wal");
        }

        private static void TryDelete(string path)
        {
            try
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
            catch (IOException)
            {
            }
        }
    }
}
