using Microsoft.Extensions.Options;

namespace ClearanceGate.Api;

public sealed class ApiAuthenticationOptionsValidator : IValidateOptions<ApiAuthenticationOptions>
{
    public ValidateOptionsResult Validate(string? name, ApiAuthenticationOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.ApiKey))
        {
            return ValidateOptionsResult.Fail("Authentication API key must not be empty.");
        }

        return ValidateOptionsResult.Success;
    }
}
