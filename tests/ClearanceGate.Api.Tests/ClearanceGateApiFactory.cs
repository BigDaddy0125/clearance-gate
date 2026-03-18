using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;

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
        builder.ConfigureAppConfiguration((_, configBuilder) =>
        {
            configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:AuditStore"] = $"Data Source={databasePath}",
            });
        });
    }
}
