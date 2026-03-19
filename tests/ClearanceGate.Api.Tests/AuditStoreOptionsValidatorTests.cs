using Microsoft.Extensions.Options;
using Xunit;

namespace ClearanceGate.Api.Tests;

public sealed class AuditStoreOptionsValidatorTests
{
    private readonly ClearanceGate.Audit.AuditStoreOptionsValidator validator = new();

    [Fact]
    public void Validate_AcceptsSqliteDataSource()
    {
        var result = validator.Validate(
            name: null,
            new ClearanceGate.Audit.AuditStoreOptions
            {
                ConnectionString = "Data Source=App_Data/clearancegate.db",
            });

        Assert.True(result.Succeeded);
    }

    [Fact]
    public void Validate_RejectsEmptyConnectionString()
    {
        var result = validator.Validate(
            name: null,
            new ClearanceGate.Audit.AuditStoreOptions
            {
                ConnectionString = "",
            });

        Assert.False(result.Succeeded);
        Assert.Contains("must not be empty", result.FailureMessage, StringComparison.Ordinal);
    }

    [Fact]
    public void Validate_RejectsConnectionStringWithoutDataSource()
    {
        var result = validator.Validate(
            name: null,
            new ClearanceGate.Audit.AuditStoreOptions
            {
                ConnectionString = "Mode=Memory",
            });

        Assert.False(result.Succeeded);
        Assert.Contains("must define a SQLite data source", result.FailureMessage, StringComparison.Ordinal);
    }
}
