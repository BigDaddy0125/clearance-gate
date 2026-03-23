using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace ClearanceGate.Api.Tests;

public sealed class ProfileDiagnosticsTests
{
    [Fact]
    public async Task ProfilesEndpoint_ReturnsEmbeddedCatalogEntries()
    {
        using var harness = CreateHarness();
        using var factory = new ClearanceGateApiFactory(harness.DatabasePath);
        using var client = factory.CreateClient();

        var response = await client.GetFromJsonAsync<ClearanceGate.Contracts.ProfileCatalogResponse>("/profiles");

        Assert.NotNull(response);
        var profile = Assert.Single(response.Profiles);
        Assert.Equal("itops_deployment_v1", profile.Profile);
        Assert.Equal("itops_deployment", profile.Family);
        Assert.Equal(1, profile.Version);
        Assert.True(profile.IsLatest);
    }

    [Fact]
    public async Task LatestProfileEndpoint_ReturnsLatestVersionForKnownFamily()
    {
        using var harness = CreateHarness();
        using var factory = new ClearanceGateApiFactory(harness.DatabasePath);
        using var client = factory.CreateClient();

        var response = await client.GetFromJsonAsync<ClearanceGate.Contracts.LatestProfileResponse>("/profiles/latest/itops_deployment");

        Assert.NotNull(response);
        Assert.Equal("itops_deployment", response.Family);
        Assert.Equal("itops_deployment_v1", response.Profile.Profile);
        Assert.True(response.Profile.IsLatest);
    }

    [Fact]
    public async Task LatestProfileEndpoint_UnknownFamilyReturnsNotFound()
    {
        using var harness = CreateHarness();
        using var factory = new ClearanceGateApiFactory(harness.DatabasePath);
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/profiles/latest/missing_family");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private static TemporaryDatabaseHarness CreateHarness()
    {
        var path = Path.Combine(Path.GetTempPath(), $"clearancegate-profile-diagnostics-{Guid.NewGuid():N}.db");
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
