namespace ClearanceGate.Api;

public sealed class ClearanceGateStartupException(string message, Exception innerException)
    : InvalidOperationException(message, innerException)
{
}
