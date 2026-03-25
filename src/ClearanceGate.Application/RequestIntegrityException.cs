namespace ClearanceGate.Application;

public sealed class RequestIntegrityException(string message)
    : InvalidOperationException(message)
{
}
