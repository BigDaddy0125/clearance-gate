using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
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
                services.AddSingleton<ClearanceGate.Profiles.IProfileCatalog>(_ => new ThrowingProfileCatalog("synthetic profile load failure"));
            });

        var exception = Assert.ThrowsAny<Exception>(() => factory.CreateClient());

        Assert.Contains("ClearanceGate startup validation failed", exception.ToString(), StringComparison.Ordinal);
        Assert.Contains("synthetic profile load failure", exception.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public void ApplicationStartup_RejectsInvalidProfileIdentity()
    {
        using var harness = CreateHarness();
        using var factory = new ClearanceGateApiFactory(
            harness.DatabasePath,
            services =>
            {
                services.AddSingleton<ClearanceGate.Profiles.IProfileCatalog>(_ =>
                    new ThrowingProfileCatalog("Profile 'bad-profile-name' must use canonical name '<family>_v<positive integer>'."));
            });

        var exception = Assert.ThrowsAny<Exception>(() => factory.CreateClient());

        Assert.Contains("ClearanceGate startup validation failed", exception.ToString(), StringComparison.Ordinal);
        Assert.Contains("must use canonical name", exception.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public void ApplicationStartup_RejectsDuplicateProfileFamilyVersion()
    {
        using var harness = CreateHarness();
        using var factory = new ClearanceGateApiFactory(
            harness.DatabasePath,
            services =>
            {
                services.AddSingleton<ClearanceGate.Profiles.IProfileCatalog>(_ =>
                    new ThrowingProfileCatalog("Profile family 'itops_deployment' defines version '1' more than once."));
            });

        var exception = Assert.ThrowsAny<Exception>(() => factory.CreateClient());

        Assert.Contains("ClearanceGate startup validation failed", exception.ToString(), StringComparison.Ordinal);
        Assert.Contains("defines version '1' more than once", exception.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public void ApplicationStartup_RejectsInvalidAuditStoreConfiguration()
    {
        using var harness = CreateHarness();
        using var factory = new ClearanceGateApiFactory(
            harness.DatabasePath,
            services =>
            {
                services.AddSingleton<IValidateOptions<ClearanceGate.Audit.AuditStoreOptions>, ClearanceGate.Audit.AuditStoreOptionsValidator>();
                services.PostConfigure<ClearanceGate.Audit.AuditStoreOptions>(options =>
                {
                    options.ConnectionString = string.Empty;
                });
            });

        var exception = Assert.ThrowsAny<Exception>(() => factory.CreateClient());

        Assert.Contains("ClearanceGate startup validation failed", exception.ToString(), StringComparison.Ordinal);
        Assert.Contains("Audit store connection string must not be empty", exception.ToString(), StringComparison.Ordinal);
    }

    private static TemporaryDatabaseHarness CreateHarness()
    {
        var path = Path.Combine(Path.GetTempPath(), $"clearancegate-startup-tests-{Guid.NewGuid():N}.db");
        return new TemporaryDatabaseHarness(path);
    }

    private sealed class ThrowingProfileCatalog : ClearanceGate.Profiles.IProfileCatalog
    {
        private readonly string message;

        public ThrowingProfileCatalog(string message)
        {
            this.message = message;
            throw new InvalidOperationException(message);
        }

        public ClearanceGate.Profiles.ClearanceProfile GetRequiredProfile(string profileName) =>
            throw new InvalidOperationException(message);
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
