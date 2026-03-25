namespace ClearanceGate.Audit;

internal sealed record AuditStoreSchemaValidation(
    string TableName,
    string[] RequiredColumns);
