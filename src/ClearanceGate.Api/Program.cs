using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;

var builder = WebApplication.CreateBuilder(args);
var configuredAuditStoreConnectionString = builder.Configuration.GetConnectionString("AuditStore")
    ?? "Data Source=App_Data/clearancegate.db";
var sqliteConnectionStringBuilder = new SqliteConnectionStringBuilder(configuredAuditStoreConnectionString);
if (!string.IsNullOrWhiteSpace(sqliteConnectionStringBuilder.DataSource) &&
    !Path.IsPathRooted(sqliteConnectionStringBuilder.DataSource))
{
    sqliteConnectionStringBuilder.DataSource = Path.Combine(
        builder.Environment.ContentRootPath,
        sqliteConnectionStringBuilder.DataSource);
}

var auditStoreConnectionString = sqliteConnectionStringBuilder.ToString();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddProblemDetails();

builder.Services.Configure<ClearanceGate.Audit.AuditStoreOptions>(options =>
{
    options.ConnectionString = auditStoreConnectionString;
});
builder.Services.AddSingleton<ClearanceGate.Profiles.IProfileCatalog, ClearanceGate.Profiles.EmbeddedProfileCatalog>();
builder.Services.AddSingleton<ClearanceGate.Policy.IProfilePolicyProjector, ClearanceGate.Policy.ProfilePolicyProjector>();
builder.Services.AddSingleton<ClearanceGate.Audit.IAuditStoreInitializer, ClearanceGate.Audit.SqliteAuditStoreInitializer>();
builder.Services.AddSingleton<ClearanceGate.Audit.IDecisionAuditStore, ClearanceGate.Audit.SqliteDecisionAuditStore>();
builder.Services.AddSingleton<ClearanceGate.Policy.IPolicyEvaluator, ClearanceGate.Policy.ItOpsDeploymentPolicyEvaluator>();
builder.Services.AddSingleton<ClearanceGate.Application.Abstractions.IAuthorizationService, ClearanceGate.Application.Services.AuthorizationService>();
builder.Services.AddSingleton<ClearanceGate.Application.Abstractions.IAcknowledgmentService, ClearanceGate.Application.Services.AcknowledgmentService>();
builder.Services.AddSingleton<ClearanceGate.Application.Abstractions.IAuditQueryService, ClearanceGate.Application.Services.AuditQueryService>();

var app = builder.Build();

await app.Services.GetRequiredService<ClearanceGate.Audit.IAuditStoreInitializer>()
    .InitializeAsync(CancellationToken.None);

app.UseExceptionHandler();

app.MapPost("/authorize", async Task<IResult> (
    ClearanceGate.Contracts.AuthorizationRequest request,
    ClearanceGate.Application.Abstractions.IAuthorizationService service,
    CancellationToken cancellationToken) =>
{
    try
    {
        var response = await service.AuthorizeAsync(request, cancellationToken);
        return TypedResults.Ok(response);
    }
    catch (KeyNotFoundException exception)
    {
        return TypedResults.BadRequest(new ProblemDetails
        {
            Title = "Authorization rejected",
            Detail = exception.Message,
            Status = StatusCodes.Status400BadRequest,
        });
    }
    catch (ArgumentException exception)
    {
        return TypedResults.BadRequest(new ProblemDetails
        {
            Title = "Authorization rejected",
            Detail = exception.Message,
            Status = StatusCodes.Status400BadRequest,
        });
    }
});

app.MapPost("/acknowledge", async Task<IResult> (
    ClearanceGate.Contracts.AcknowledgmentRequest request,
    ClearanceGate.Application.Abstractions.IAcknowledgmentService service,
    CancellationToken cancellationToken) =>
{
    try
    {
        var response = await service.AcknowledgeAsync(request, cancellationToken);
        return TypedResults.Ok(response);
    }
    catch (KeyNotFoundException)
    {
        return TypedResults.NotFound();
    }
    catch (ArgumentException exception)
    {
        return TypedResults.BadRequest(new ProblemDetails
        {
            Title = "Acknowledgment rejected",
            Detail = exception.Message,
            Status = StatusCodes.Status400BadRequest,
        });
    }
    catch (InvalidOperationException exception)
    {
        return TypedResults.Conflict(new ProblemDetails
        {
            Title = "Acknowledgment rejected",
            Detail = exception.Message,
            Status = StatusCodes.Status409Conflict,
        });
    }
});

app.MapGet("/audit/{decisionId}",
    async Task<IResult> (
        string decisionId,
        ClearanceGate.Application.Abstractions.IAuditQueryService service,
        CancellationToken cancellationToken) =>
    {
        var response = await service.GetAuditAsync(decisionId, cancellationToken);
        return response is null ? TypedResults.NotFound() : TypedResults.Ok(response);
    });

app.Run();

public partial class Program;
