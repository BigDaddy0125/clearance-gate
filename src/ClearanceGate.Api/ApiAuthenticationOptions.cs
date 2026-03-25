using Microsoft.AspNetCore.Authentication;

namespace ClearanceGate.Api;

public sealed class ApiAuthenticationOptions : AuthenticationSchemeOptions
{
    public const string SectionName = "Authentication";
    public const string SchemeName = "ApiKeyBearer";

    public string ApiKey { get; set; } = string.Empty;
}
