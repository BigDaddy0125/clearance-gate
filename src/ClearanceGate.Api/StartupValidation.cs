using Microsoft.Extensions.Options;

namespace ClearanceGate.Api;

public static class StartupValidation
{
    public static async Task ValidateAsync(
        IServiceProvider services,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        try
        {
            logger.LogInformation("Starting fail-closed startup validation.");

            var auditStoreOptions = services.GetRequiredService<IOptions<ClearanceGate.Audit.AuditStoreOptions>>().Value;
            logger.LogInformation(
                "Audit store configuration resolved for startup validation. ConnectionStringPresent={ConnectionStringPresent}",
                !string.IsNullOrWhiteSpace(auditStoreOptions.ConnectionString));

            // Force profile catalog construction at startup so invalid embedded profiles fail closed.
            _ = services.GetRequiredService<ClearanceGate.Profiles.IProfileCatalog>();
            logger.LogInformation("Embedded profile catalog loaded during startup validation.");

            await services.GetRequiredService<ClearanceGate.Audit.IAuditStoreInitializer>()
                .InitializeAsync(cancellationToken);

            logger.LogInformation("Startup validation completed successfully.");
        }
        catch (Exception exception) when (exception is not ClearanceGateStartupException)
        {
            logger.LogCritical(exception, "Startup validation failed.");
            throw new ClearanceGateStartupException(
                "ClearanceGate startup validation failed. See inner exception for the rejected boundary condition.",
                exception);
        }
    }
}
