using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Options;

namespace ClearanceGate.Audit;

public sealed class AuditStoreOptionsValidator : IValidateOptions<AuditStoreOptions>
{
    public ValidateOptionsResult Validate(string? name, AuditStoreOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.ConnectionString))
        {
            return ValidateOptionsResult.Fail("Audit store connection string must not be empty.");
        }

        SqliteConnectionStringBuilder builder;
        try
        {
            builder = new SqliteConnectionStringBuilder(options.ConnectionString);
        }
        catch (ArgumentException exception)
        {
            return ValidateOptionsResult.Fail($"Audit store connection string is invalid: {exception.Message}");
        }

        if (string.IsNullOrWhiteSpace(builder.DataSource))
        {
            return ValidateOptionsResult.Fail("Audit store connection string must define a SQLite data source.");
        }

        return ValidateOptionsResult.Success;
    }
}
