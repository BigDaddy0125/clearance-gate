using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace ClearanceGate.Api.Tests;

public sealed class StartupFailureTests
{
    [Fact]
    public void ApplicationStartup_RejectsInvalidProfileCatalog()
    {
        using var harness = CreateHarness();
        using var factory = new ClearanceGateApiFactory(
            harness.DatabasePath,
            services =>
            {
                services.AddSingleton<ClearanceGate.Profiles.IProfileCatalog>(_ => new ThrowingProfileCatalog());
            });

        var exception = Assert.ThrowsAny<Exception>(() => factory.CreateClient());

        Assert.Contains("ClearanceGate startup validation failed", exception.ToString(), StringComparison.Ordinal);
        Assert.Contains("synthetic profile load failure", exception.ToString(), StringComparison.Ordinal);
    }

    private static TemporaryDatabaseHarness CreateHarness()
    {
        var path = Path.Combine(Path.GetTempPath(), $"clearancegate-startup-tests-{Guid.NewGuid():N}.db");
        return new TemporaryDatabaseHarness(path);
    }

    private sealed class ThrowingProfileCatalog : ClearanceGate.Profiles.IProfileCatalog
    {
        public ThrowingProfileCatalog()
        {
            throw new InvalidOperationException("synthetic profile load failure");
        }

        public ClearanceGate.Profiles.ClearanceProfile GetRequiredProfile(string profileName) =>
            throw new InvalidOperationException("synthetic profile load failure");
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
