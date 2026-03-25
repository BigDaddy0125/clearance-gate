using System.Net.Http.Headers;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace ClearanceGate.Api.Tests;

public sealed class ClearanceGateApiFactory : WebApplicationFactory<Program>
{
    public const string TestApiKey = "clearancegate-test-api-key";

    private readonly string databasePath;
    private readonly Action<IServiceCollection>? configureTestServices;

    public ClearanceGateApiFactory(string databasePath, Action<IServiceCollection>? configureTestServices = null)
    {
        this.databasePath = databasePath;
        this.configureTestServices = configureTestServices;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            services.PostConfigure<ClearanceGate.Audit.AuditStoreOptions>(options =>
            {
                options.ConnectionString = $"Data Source={databasePath}";
            });
            services.PostConfigureAll<ClearanceGate.Api.ApiAuthenticationOptions>(options =>
            {
                options.ApiKey = TestApiKey;
            });

            configureTestServices?.Invoke(services);
        });
    }

    protected override void ConfigureClient(HttpClient client)
    {
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", TestApiKey);
    }
}
