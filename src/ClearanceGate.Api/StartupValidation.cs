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
            // Force profile catalog construction at startup so invalid embedded profiles fail closed.
            _ = services.GetRequiredService<ClearanceGate.Profiles.IProfileCatalog>();

            await services.GetRequiredService<ClearanceGate.Audit.IAuditStoreInitializer>()
                .InitializeAsync(cancellationToken);
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
