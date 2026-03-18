namespace ClearanceGate.Audit;

public interface IAuditStoreInitializer
{
    Task InitializeAsync(CancellationToken cancellationToken);
}
