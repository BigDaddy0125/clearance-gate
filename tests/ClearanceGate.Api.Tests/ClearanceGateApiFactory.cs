using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace ClearanceGate.Api.Tests;

public sealed class ClearanceGateApiFactory : WebApplicationFactory<Program>
{
    private readonly string databasePath;

    public ClearanceGateApiFactory(string databasePath)
    {
        this.databasePath = databasePath;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            services.PostConfigure<ClearanceGate.Audit.AuditStoreOptions>(options =>
            {
                options.ConnectionString = $"Data Source={databasePath}";
            });
        });
    }
}
