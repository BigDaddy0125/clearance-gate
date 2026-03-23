using Microsoft.Extensions.Options;

namespace ClearanceGate.Api;

public static class StartupValidation
{
    private static class LogEvents
    {
        public static readonly EventId ValidationStarted = new(1000, nameof(ValidationStarted));
        public static readonly EventId AuditStoreOptionsResolved = new(1001, nameof(AuditStoreOptionsResolved));
        public static readonly EventId ProfileCatalogLoaded = new(1002, nameof(ProfileCatalogLoaded));
        public static readonly EventId ValidationCompleted = new(1003, nameof(ValidationCompleted));
        public static readonly EventId ValidationFailed = new(1004, nameof(ValidationFailed));
    }

    public static async Task ValidateAsync(
        IServiceProvider services,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        try
        {
            logger.LogInformation(LogEvents.ValidationStarted, "Starting fail-closed startup validation.");

            var auditStoreOptions = services.GetRequiredService<IOptions<ClearanceGate.Audit.AuditStoreOptions>>().Value;
            logger.LogInformation(
                LogEvents.AuditStoreOptionsResolved,
                "Audit store configuration resolved for startup validation. ConnectionStringPresent={ConnectionStringPresent}",
                !string.IsNullOrWhiteSpace(auditStoreOptions.ConnectionString));

            // Force profile catalog construction at startup so invalid embedded profiles fail closed.
            _ = services.GetRequiredService<ClearanceGate.Profiles.IProfileCatalog>();
            logger.LogInformation(LogEvents.ProfileCatalogLoaded, "Embedded profile catalog loaded during startup validation.");

            await services.GetRequiredService<ClearanceGate.Audit.IAuditStoreInitializer>()
                .InitializeAsync(cancellationToken);

            logger.LogInformation(LogEvents.ValidationCompleted, "Startup validation completed successfully.");
        }
        catch (Exception exception) when (exception is not ClearanceGateStartupException)
        {
            logger.LogCritical(LogEvents.ValidationFailed, exception, "Startup validation failed.");
            throw new ClearanceGateStartupException(
                "ClearanceGate startup validation failed. See inner exception for the rejected boundary condition.",
                exception);
        }
    }
}
