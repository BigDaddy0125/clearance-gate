namespace ClearanceGate.Application.Abstractions;

public interface IAuthorizationService
{
    Task<ClearanceGate.Contracts.AuthorizationResponse> AuthorizeAsync(
        ClearanceGate.Contracts.AuthorizationRequest request,
        CancellationToken cancellationToken);
}
