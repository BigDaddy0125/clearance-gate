using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace ClearanceGate.Api.Tests;

public sealed class ApiContractTests
{
    [Fact]
    public async Task Requests_RequireBearerApiKey()
    {
        using var harness = CreateHarness();
        using var factory = new ClearanceGateApiFactory(harness.DatabasePath);
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = null;

        var response = await client.GetAsync("/profiles");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task InvalidBearerApiKey_IsRejected()
    {
        using var harness = CreateHarness();
        using var factory = new ClearanceGateApiFactory(harness.DatabasePath);
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "wrong-key");

        var response = await client.GetAsync("/profiles");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task AuthorizationRejectsNonUtcTimestamp()
    {
        using var harness = CreateHarness();
        using var factory = new ClearanceGateApiFactory(harness.DatabasePath);
        using var client = factory.CreateClient();

        var request = BuildAuthorizationRequest("req-contract-1", "dec-contract-1") with
        {
            Metadata = new ClearanceGate.Contracts.RequestMetadata("change-control", "2026-03-18T10:00:00")
        };

        var response = await client.PostAsJsonAsync("/authorize", request);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.NotNull(problem);
        Assert.Contains("UTC ISO 8601", problem.Detail, StringComparison.Ordinal);
    }

    [Fact]
    public async Task AcknowledgmentRejectsNonUtcTimestamp()
    {
        using var harness = CreateHarness();
        using var factory = new ClearanceGateApiFactory(harness.DatabasePath);
        using var client = factory.CreateClient();

        var authorizeResponse = await client.PostAsJsonAsync("/authorize", BuildAuthorizationRequest("req-contract-2", "dec-contract-2"));
        Assert.Equal(HttpStatusCode.OK, authorizeResponse.StatusCode);

        var response = await client.PostAsJsonAsync("/acknowledge", new ClearanceGate.Contracts.AcknowledgmentRequest(
            "dec-contract-2",
            new ClearanceGate.Contracts.Acknowledger("alice", "acknowledging_authority"),
            new ClearanceGate.Contracts.AcknowledgmentPayload("risk_acceptance", "2026-03-18 10:05:00")));
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.NotNull(problem);
        Assert.Contains("UTC ISO 8601", problem.Detail, StringComparison.Ordinal);
    }

    [Fact]
    public async Task DuplicateRequestIdWithDifferentDecision_IsRejected()
    {
        using var harness = CreateHarness();
        using var factory = new ClearanceGateApiFactory(harness.DatabasePath);
        using var client = factory.CreateClient();

        var firstResponse = await client.PostAsJsonAsync("/authorize", BuildAuthorizationRequest("req-contract-3", "dec-contract-3a"));
        var secondResponse = await client.PostAsJsonAsync("/authorize", BuildAuthorizationRequest("req-contract-3", "dec-contract-3b"));
        var problem = await secondResponse.Content.ReadFromJsonAsync<ProblemDetails>();

        Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, secondResponse.StatusCode);
        Assert.NotNull(problem);
        Assert.Contains("already bound", problem.Detail, StringComparison.Ordinal);
    }

    [Fact]
    public async Task DuplicateRequestIdWithDifferentSourceSystem_IsRejected()
    {
        using var harness = CreateHarness();
        using var factory = new ClearanceGateApiFactory(harness.DatabasePath);
        using var client = factory.CreateClient();

        var canonical = BuildAuthorizationRequest("req-contract-5", "dec-contract-5");
        var conflicting = canonical with
        {
            Metadata = canonical.Metadata with
            {
                SourceSystem = "deployment-override",
            },
        };

        var firstResponse = await client.PostAsJsonAsync("/authorize", canonical);
        var secondResponse = await client.PostAsJsonAsync("/authorize", conflicting);
        var problem = await secondResponse.Content.ReadFromJsonAsync<ProblemDetails>();

        Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, secondResponse.StatusCode);
        Assert.NotNull(problem);
        Assert.Contains("already bound", problem.Detail, StringComparison.Ordinal);
    }

    private static ClearanceGate.Contracts.AuthorizationRequest BuildAuthorizationRequest(string requestId, string decisionId) =>
        new(
            requestId,
            decisionId,
            "itops_deployment_v1",
            new ClearanceGate.Contracts.ActionDescriptor("deploy", "Promote release 2026.03.18"),
            new ClearanceGate.Contracts.DecisionContext(new Dictionary<string, string>
            {
                ["changeWindow"] = "business-hours",
            }),
            new[] { "HIGH_IMPACT" },
            new ClearanceGate.Contracts.ResponsibilityDescriptor("alice", "decision_owner"),
            new ClearanceGate.Contracts.RequestMetadata("change-control", "2026-03-18T10:00:00Z"));

    private static TemporaryDatabaseHarness CreateHarness()
    {
        var path = Path.Combine(Path.GetTempPath(), $"clearancegate-contract-tests-{Guid.NewGuid():N}.db");
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
