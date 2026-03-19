using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace ClearanceGate.Api.Tests;

public sealed class AuthorizationClaimsTests
{
    // CG3: bounded acknowledgment
    [Fact]
    public async Task RiskFlaggedDecision_RequiresAck_ThenAuditShowsAuthorizedAfterAck()
    {
        using var harness = CreateHarness();
        using var factory = new ClearanceGateApiFactory(harness.DatabasePath);
        using var client = factory.CreateClient();
        var request = BuildAuthorizationRequest("req-claim-1", "dec-claim-1", new[] { "HIGH_IMPACT" }, "alice", "change-control");

        var authorizeResponse = await client.PostAsJsonAsync("/authorize", request);
        var authorizePayload = await authorizeResponse.Content.ReadFromJsonAsync<ClearanceGate.Contracts.AuthorizationResponse>();

        var acknowledgeResponse = await client.PostAsJsonAsync("/acknowledge", new ClearanceGate.Contracts.AcknowledgmentRequest(
            "dec-claim-1",
            new ClearanceGate.Contracts.Acknowledger("alice", "acknowledging_authority"),
            new ClearanceGate.Contracts.AcknowledgmentPayload("risk_acceptance", "2026-03-18T10:05:00Z")));

        var auditPayload = await client.GetFromJsonAsync<ClearanceGate.Contracts.AuditRecordResponse>("/audit/dec-claim-1");

        Assert.Equal(HttpStatusCode.OK, authorizeResponse.StatusCode);
        Assert.NotNull(authorizePayload);
        Assert.Equal("REQUIRE_ACK", authorizePayload.Outcome);
        Assert.Equal("AWAITING_ACK", authorizePayload.ClearanceState);

        Assert.Equal(HttpStatusCode.OK, acknowledgeResponse.StatusCode);
        Assert.NotNull(auditPayload);
        Assert.Equal("PROCEED", auditPayload.Outcome);
        Assert.Equal(new[] { "AWAITING_ACK", "AUTHORIZED" }, auditPayload.AuthorizationTimeline.Select(item => item.State));
    }

    // CG2 and CG3: fail-closed state plus bounded acknowledgment
    [Fact]
    public async Task BlockedDecision_CannotBeReleasedByAcknowledgment()
    {
        using var harness = CreateHarness();
        using var factory = new ClearanceGateApiFactory(harness.DatabasePath);
        using var client = factory.CreateClient();
        var request = BuildAuthorizationRequest("req-claim-2", "dec-claim-2", Array.Empty<string>(), string.Empty, string.Empty);

        var authorizeResponse = await client.PostAsJsonAsync("/authorize", request);
        var acknowledgeResponse = await client.PostAsJsonAsync("/acknowledge", new ClearanceGate.Contracts.AcknowledgmentRequest(
            "dec-claim-2",
            new ClearanceGate.Contracts.Acknowledger("alice", "acknowledging_authority"),
            new ClearanceGate.Contracts.AcknowledgmentPayload("risk_acceptance", "2026-03-18T10:05:00Z")));

        var problem = await acknowledgeResponse.Content.ReadFromJsonAsync<ProblemDetails>();
        var auditPayload = await client.GetFromJsonAsync<ClearanceGate.Contracts.AuditRecordResponse>("/audit/dec-claim-2");

        Assert.Equal(HttpStatusCode.OK, authorizeResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, acknowledgeResponse.StatusCode);
        Assert.NotNull(problem);
        Assert.Equal("Acknowledgment rejected", problem.Title);
        Assert.NotNull(auditPayload);
        Assert.Equal("BLOCK", auditPayload.Outcome);
        Assert.Single(auditPayload.AuthorizationTimeline);
        Assert.Equal("INFO_INSUFFICIENT", auditPayload.AuthorizationTimeline[0].State);
    }

    // CG6: profile required-field constraint projects to runtime enforcement
    [Fact]
    public async Task MissingSourceSystem_MapsToProfileRequiredFieldConstraint()
    {
        using var harness = CreateHarness();
        using var factory = new ClearanceGateApiFactory(harness.DatabasePath);
        using var client = factory.CreateClient();
        var request = BuildAuthorizationRequest("req-claim-2b", "dec-claim-2b", Array.Empty<string>(), "alice", string.Empty);

        var authorizeResponse = await client.PostAsJsonAsync("/authorize", request);
        var authorizePayload = await authorizeResponse.Content.ReadFromJsonAsync<ClearanceGate.Contracts.AuthorizationResponse>();

        Assert.Equal(HttpStatusCode.OK, authorizeResponse.StatusCode);
        Assert.NotNull(authorizePayload);
        Assert.Equal("BLOCK", authorizePayload.Outcome);
        Assert.Contains("SOURCE_REQUIRED", authorizePayload.Reason.ConstraintsTriggered);
    }

    // CG5: same request id replays to the first durable decision
    [Fact]
    public async Task SameRequestId_RemainsIdempotentAcrossConcurrentRequests()
    {
        using var harness = CreateHarness();
        using var factory = new ClearanceGateApiFactory(harness.DatabasePath);
        using var client = factory.CreateClient();
        var requests = Enumerable.Range(0, 8)
            .Select(index => BuildAuthorizationRequest(
                "req-claim-3",
                $"dec-claim-3-{index}",
                new[] { "HIGH_IMPACT" },
                "alice",
                "change-control"))
            .ToArray();

        var responses = await Task.WhenAll(requests.Select(request => client.PostAsJsonAsync("/authorize", request)));
        var payloads = await Task.WhenAll(responses.Select(response => response.Content.ReadFromJsonAsync<ClearanceGate.Contracts.AuthorizationResponse>()));

        Assert.All(responses, response => Assert.Equal(HttpStatusCode.OK, response.StatusCode));
        Assert.DoesNotContain(payloads, payload => payload is null);

        var decisionIds = payloads.Select(payload => payload!.DecisionId).Distinct(StringComparer.Ordinal).ToArray();
        Assert.Single(decisionIds);

        var auditPayload = await client.GetFromJsonAsync<ClearanceGate.Contracts.AuditRecordResponse>($"/audit/{decisionIds[0]}");
        Assert.NotNull(auditPayload);
        Assert.Single(auditPayload.AuthorizationTimeline);
    }

    // CG4: durable evidence remains reconstructable after restart
    [Fact]
    public async Task AuditEvidence_RemainsReadableAfterApplicationRestart()
    {
        using var harness = CreateHarness();

        using (var firstFactory = new ClearanceGateApiFactory(harness.DatabasePath))
        using (var firstClient = firstFactory.CreateClient())
        {
            var request = BuildAuthorizationRequest("req-claim-4", "dec-claim-4", new[] { "HIGH_IMPACT" }, "alice", "change-control");
            var authorizeResponse = await firstClient.PostAsJsonAsync("/authorize", request);

            Assert.Equal(HttpStatusCode.OK, authorizeResponse.StatusCode);
        }

        using (var secondFactory = new ClearanceGateApiFactory(harness.DatabasePath))
        using (var secondClient = secondFactory.CreateClient())
        {
            var auditPayload = await secondClient.GetFromJsonAsync<ClearanceGate.Contracts.AuditRecordResponse>("/audit/dec-claim-4");

            Assert.NotNull(auditPayload);
            Assert.Equal("evidence:dec-claim-4", auditPayload.EvidenceId);
            Assert.Equal("REQUIRE_ACK", auditPayload.Outcome);
            Assert.Single(auditPayload.AuthorizationTimeline);
            Assert.Equal("AWAITING_ACK", auditPayload.AuthorizationTimeline[0].State);
        }
    }

    // CG4: a non-blocking outcome is immediately backed by durable evidence and timeline
    [Fact]
    public async Task NonBlockingOutcome_ImmediatelyExposesDurableEvidenceInAudit()
    {
        using var harness = CreateHarness();
        using var factory = new ClearanceGateApiFactory(harness.DatabasePath);
        using var client = factory.CreateClient();
        var request = BuildAuthorizationRequest("req-claim-4b", "dec-claim-4b", new[] { "HIGH_IMPACT" }, "alice", "change-control");

        var authorizeResponse = await client.PostAsJsonAsync("/authorize", request);
        var authorizePayload = await authorizeResponse.Content.ReadFromJsonAsync<ClearanceGate.Contracts.AuthorizationResponse>();
        var auditPayload = await client.GetFromJsonAsync<ClearanceGate.Contracts.AuditRecordResponse>("/audit/dec-claim-4b");

        Assert.Equal(HttpStatusCode.OK, authorizeResponse.StatusCode);
        Assert.NotNull(authorizePayload);
        Assert.NotNull(auditPayload);
        Assert.Equal("REQUIRE_ACK", authorizePayload.Outcome);
        Assert.Equal(authorizePayload.EvidenceId, auditPayload.EvidenceId);
        Assert.Equal(authorizePayload.DecisionId, auditPayload.DecisionId);
        Assert.Single(auditPayload.AuthorizationTimeline);
        Assert.Equal("AWAITING_ACK", auditPayload.AuthorizationTimeline[0].State);
    }

    [Fact]
    public async Task AuditExport_ReturnsReconstructableDecisionEnvelope()
    {
        using var harness = CreateHarness();
        using var factory = new ClearanceGateApiFactory(harness.DatabasePath);
        using var client = factory.CreateClient();
        var request = BuildAuthorizationRequest("req-claim-export-1", "dec-claim-export-1", new[] { "HIGH_IMPACT" }, "alice", "change-control");

        var authorizeResponse = await client.PostAsJsonAsync("/authorize", request);
        var exportPayload = await client.GetFromJsonAsync<ClearanceGate.Contracts.AuditExportResponse>("/audit/dec-claim-export-1/export");

        Assert.Equal(HttpStatusCode.OK, authorizeResponse.StatusCode);
        Assert.NotNull(exportPayload);
        Assert.Equal("dec-claim-export-1", exportPayload.DecisionId);
        Assert.Equal("req-claim-export-1", exportPayload.RequestId);
        Assert.Equal("itops_deployment_v1", exportPayload.Profile);
        Assert.Equal("evidence:dec-claim-export-1", exportPayload.EvidenceId);
        Assert.Equal("REQUIRE_ACK", exportPayload.Outcome);
        Assert.Equal("AWAITING_ACK", exportPayload.ClearanceState);
        Assert.Contains("RISK_ACK_REQUIRED", exportPayload.ConstraintsApplied);
        Assert.Single(exportPayload.AuthorizationTimeline);
        Assert.Equal("AWAITING_ACK", exportPayload.AuthorizationTimeline[0].State);
    }

    [Fact]
    public async Task AuditByRequestId_ResolvesCanonicalDecisionAndExport()
    {
        using var harness = CreateHarness();
        using var factory = new ClearanceGateApiFactory(harness.DatabasePath);
        using var client = factory.CreateClient();
        var requests = Enumerable.Range(0, 3)
            .Select(index => BuildAuthorizationRequest(
                "req-claim-export-2",
                $"dec-claim-export-2-{index}",
                new[] { "HIGH_IMPACT" },
                "alice",
                "change-control"))
            .ToArray();

        var responses = await Task.WhenAll(requests.Select(request => client.PostAsJsonAsync("/authorize", request)));
        var payloads = await Task.WhenAll(responses.Select(response => response.Content.ReadFromJsonAsync<ClearanceGate.Contracts.AuthorizationResponse>()));
        var canonicalDecisionId = payloads.Select(payload => payload!.DecisionId).Distinct(StringComparer.Ordinal).Single();

        var auditPayload = await client.GetFromJsonAsync<ClearanceGate.Contracts.AuditRecordResponse>("/audit/request/req-claim-export-2");
        var exportPayload = await client.GetFromJsonAsync<ClearanceGate.Contracts.AuditExportResponse>("/audit/request/req-claim-export-2/export");

        Assert.All(responses, response => Assert.Equal(HttpStatusCode.OK, response.StatusCode));
        Assert.NotNull(auditPayload);
        Assert.NotNull(exportPayload);
        Assert.Equal(canonicalDecisionId, auditPayload.DecisionId);
        Assert.Equal(canonicalDecisionId, exportPayload.DecisionId);
        Assert.Equal("req-claim-export-2", exportPayload.RequestId);
        Assert.Equal("REQUIRE_ACK", exportPayload.Outcome);
    }

    // CG6: unknown profile is rejected fail-closed
    [Fact]
    public async Task UnknownProfile_IsRejectedFailClosed()
    {
        using var harness = CreateHarness();
        using var factory = new ClearanceGateApiFactory(harness.DatabasePath);
        using var client = factory.CreateClient();
        var request = BuildAuthorizationRequest("req-claim-5", "dec-claim-5", Array.Empty<string>(), "alice", "change-control") with
        {
            Profile = "nonexistent_profile_v1",
        };

        var authorizeResponse = await client.PostAsJsonAsync("/authorize", request);
        var problem = await authorizeResponse.Content.ReadFromJsonAsync<ProblemDetails>();

        Assert.Equal(HttpStatusCode.BadRequest, authorizeResponse.StatusCode);
        Assert.NotNull(problem);
        Assert.Equal("Authorization rejected", problem.Title);

        var auditResponse = await client.GetAsync("/audit/dec-claim-5");
        Assert.Equal(HttpStatusCode.NotFound, auditResponse.StatusCode);
    }

    // CG6: authorization role must conform to the profile/kernel boundary
    [Fact]
    public async Task AuthorizationRole_MustMatchProfileRequirement()
    {
        using var harness = CreateHarness();
        using var factory = new ClearanceGateApiFactory(harness.DatabasePath);
        using var client = factory.CreateClient();
        var request = BuildAuthorizationRequest("req-claim-6", "dec-claim-6", Array.Empty<string>(), "alice", "change-control") with
        {
            Responsibility = new ClearanceGate.Contracts.ResponsibilityDescriptor("alice", "release-manager"),
        };

        var authorizeResponse = await client.PostAsJsonAsync("/authorize", request);
        var problem = await authorizeResponse.Content.ReadFromJsonAsync<ProblemDetails>();

        Assert.Equal(HttpStatusCode.BadRequest, authorizeResponse.StatusCode);
        Assert.NotNull(problem);
        Assert.Equal("Authorization rejected", problem.Title);
    }

    // CG6: acknowledgment role must conform to the profile/kernel boundary
    [Fact]
    public async Task AcknowledgmentRole_MustMatchProfileRequirement()
    {
        using var harness = CreateHarness();
        using var factory = new ClearanceGateApiFactory(harness.DatabasePath);
        using var client = factory.CreateClient();
        var request = BuildAuthorizationRequest(
            "req-claim-7",
            "dec-claim-7",
            new[] { "HIGH_IMPACT" },
            "alice",
            "change-control") with
        {
            Responsibility = new ClearanceGate.Contracts.ResponsibilityDescriptor("alice", "decision_owner"),
        };

        var authorizeResponse = await client.PostAsJsonAsync("/authorize", request);
        Assert.Equal(HttpStatusCode.OK, authorizeResponse.StatusCode);

        var acknowledgeResponse = await client.PostAsJsonAsync("/acknowledge", new ClearanceGate.Contracts.AcknowledgmentRequest(
            "dec-claim-7",
            new ClearanceGate.Contracts.Acknowledger("alice", "release-manager"),
            new ClearanceGate.Contracts.AcknowledgmentPayload("risk_acceptance", "2026-03-18T10:05:00Z")));
        var problem = await acknowledgeResponse.Content.ReadFromJsonAsync<ProblemDetails>();

        Assert.Equal(HttpStatusCode.BadRequest, acknowledgeResponse.StatusCode);
        Assert.NotNull(problem);
        Assert.Equal("Acknowledgment rejected", problem.Title);

        var auditPayload = await client.GetFromJsonAsync<ClearanceGate.Contracts.AuditRecordResponse>("/audit/dec-claim-7");
        Assert.NotNull(auditPayload);
        Assert.Equal("REQUIRE_ACK", auditPayload.Outcome);
        Assert.Single(auditPayload.AuthorizationTimeline);
        Assert.Equal("AWAITING_ACK", auditPayload.AuthorizationTimeline[0].State);
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
        var path = Path.Combine(Path.GetTempPath(), $"clearancegate-tests-{Guid.NewGuid():N}.db");
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
