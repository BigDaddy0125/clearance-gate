using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace ClearanceGate.Application;

public static class RequestFingerprintCalculator
{
    public static string Compute(ClearanceGate.Contracts.AuthorizationRequest request)
    {
        var normalized = new
        {
            request.RequestId,
            request.DecisionId,
            request.Profile,
            Action = new
            {
                request.Action.Type,
                request.Action.Description,
            },
            ContextAttributes = request.Context.Attributes
                .OrderBy(pair => pair.Key, StringComparer.Ordinal)
                .Select(pair => new
                {
                    pair.Key,
                    pair.Value,
                }),
            RiskFlags = request.RiskFlags
                .OrderBy(flag => flag, StringComparer.Ordinal),
            Responsibility = new
            {
                request.Responsibility.Owner,
                request.Responsibility.Role,
            },
            Metadata = new
            {
                request.Metadata.SourceSystem,
                request.Metadata.Timestamp,
            },
        };

        var json = JsonSerializer.Serialize(normalized);
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(json));
        return Convert.ToHexString(hash);
    }
}
