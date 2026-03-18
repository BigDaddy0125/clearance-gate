using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace ClearanceGate.Api.Tests;

public sealed class AuthorizationClaimsTests
{
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
            new ClearanceGate.Contracts.Acknowledger("alice", "release-manager"),
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
            new ClearanceGate.Contracts.Acknowledger("alice", "release-manager"),
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
            Assert.Equal("REQUIRE_ACK", auditPayload.Outcome);
            Assert.Single(auditPayload.AuthorizationTimeline);
            Assert.Equal("AWAITING_ACK", auditPayload.AuthorizationTimeline[0].State);
        }
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
            new ClearanceGate.Contracts.ResponsibilityDescriptor(owner, "release-manager"),
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
