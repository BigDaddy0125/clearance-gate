using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace ClearanceGate.Api;

public sealed class ApiKeyAuthenticationHandler(
    IOptionsMonitor<ApiAuthenticationOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder)
    : AuthenticationHandler<ApiAuthenticationOptions>(options, logger, encoder)
{
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue("Authorization", out var authorizationValues))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var authorizationHeader = authorizationValues.ToString();
        const string prefix = "Bearer ";
        if (!authorizationHeader.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult(AuthenticateResult.Fail("Authorization header must use the Bearer scheme."));
        }

        var providedKey = authorizationHeader[prefix.Length..].Trim();
        if (string.IsNullOrWhiteSpace(providedKey))
        {
            return Task.FromResult(AuthenticateResult.Fail("Authorization header is missing the API key value."));
        }

        if (!string.Equals(providedKey, Options.ApiKey, StringComparison.Ordinal))
        {
            return Task.FromResult(AuthenticateResult.Fail("Authorization header contains an invalid API key."));
        }

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "clearancegate-api-client"),
            new Claim(ClaimTypes.Name, "clearancegate-api-client"),
        };
        var identity = new ClaimsIdentity(claims, ApiAuthenticationOptions.SchemeName);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, ApiAuthenticationOptions.SchemeName);
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
