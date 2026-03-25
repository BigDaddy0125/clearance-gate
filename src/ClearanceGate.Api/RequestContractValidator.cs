using System.Globalization;

namespace ClearanceGate.Api;

public static class RequestContractValidator
{
    public static void Validate(ClearanceGate.Contracts.AuthorizationRequest request)
    {
        RequireValue(request.RequestId, "requestId");
        RequireValue(request.DecisionId, "decisionId");
        RequireValue(request.Profile, "profile");
        RequireValue(request.Action.Type, "action.type");
        RequireValue(request.Action.Description, "action.description");
        RequireValue(request.Responsibility.Owner, "responsibility.owner");
        RequireValue(request.Responsibility.Role, "responsibility.role");
        RequireValue(request.Metadata.SourceSystem, "metadata.sourceSystem");
        RequireUtcTimestamp(request.Metadata.Timestamp, "metadata.timestamp");
    }

    public static void Validate(ClearanceGate.Contracts.AcknowledgmentRequest request)
    {
        RequireValue(request.DecisionId, "decisionId");
        RequireValue(request.Acknowledger.Id, "acknowledger.id");
        RequireValue(request.Acknowledger.Role, "acknowledger.role");
        RequireValue(request.Acknowledgment.Type, "acknowledgment.type");
        RequireUtcTimestamp(request.Acknowledgment.Timestamp, "acknowledgment.timestamp");
    }

    private static void RequireValue(string value, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException($"Field '{fieldName}' must not be empty.");
        }
    }

    private static void RequireUtcTimestamp(string value, string fieldName)
    {
        if (!DateTimeOffset.TryParse(
                value,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                out var timestamp) ||
            (!value.EndsWith("Z", StringComparison.OrdinalIgnoreCase) &&
             !value.EndsWith("+00:00", StringComparison.Ordinal)) ||
            timestamp.Offset != TimeSpan.Zero)
        {
            throw new ArgumentException($"Field '{fieldName}' must be a UTC ISO 8601 timestamp.");
        }
    }
}
