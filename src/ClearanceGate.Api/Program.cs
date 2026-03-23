using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);
var configuredAuditStoreConnectionString = builder.Configuration.GetConnectionString("AuditStore");
var auditStoreConnectionString = string.Empty;
if (!string.IsNullOrWhiteSpace(configuredAuditStoreConnectionString))
{
    var sqliteConnectionStringBuilder = new SqliteConnectionStringBuilder(configuredAuditStoreConnectionString);
    if (!string.IsNullOrWhiteSpace(sqliteConnectionStringBuilder.DataSource) &&
        !Path.IsPathRooted(sqliteConnectionStringBuilder.DataSource))
    {
        sqliteConnectionStringBuilder.DataSource = Path.Combine(
            builder.Environment.ContentRootPath,
            sqliteConnectionStringBuilder.DataSource);
    }

    auditStoreConnectionString = sqliteConnectionStringBuilder.ToString();
}

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddProblemDetails();

builder.Services.Configure<ClearanceGate.Audit.AuditStoreOptions>(options =>
{
    options.ConnectionString = auditStoreConnectionString;
});
builder.Services.AddSingleton<IValidateOptions<ClearanceGate.Audit.AuditStoreOptions>, ClearanceGate.Audit.AuditStoreOptionsValidator>();
builder.Services.AddSingleton<ClearanceGate.Profiles.IProfileCatalog, ClearanceGate.Profiles.EmbeddedProfileCatalog>();
builder.Services.AddSingleton<ClearanceGate.Policy.IProfilePolicyProjector, ClearanceGate.Policy.ProfilePolicyProjector>();
builder.Services.AddSingleton<ClearanceGate.Audit.IAuditStoreInitializer, ClearanceGate.Audit.SqliteAuditStoreInitializer>();
builder.Services.AddSingleton<ClearanceGate.Audit.IDecisionAuditStore, ClearanceGate.Audit.SqliteDecisionAuditStore>();
builder.Services.AddSingleton<ClearanceGate.Policy.IPolicyEvaluator, ClearanceGate.Policy.ItOpsDeploymentPolicyEvaluator>();
builder.Services.AddSingleton<ClearanceGate.Application.Abstractions.IAuthorizationService, ClearanceGate.Application.Services.AuthorizationService>();
builder.Services.AddSingleton<ClearanceGate.Application.Abstractions.IAcknowledgmentService, ClearanceGate.Application.Services.AcknowledgmentService>();
builder.Services.AddSingleton<ClearanceGate.Application.Abstractions.IAuditQueryService, ClearanceGate.Application.Services.AuditQueryService>();

var app = builder.Build();

await ClearanceGate.Api.StartupValidation.ValidateAsync(
    app.Services,
    app.Logger,
    CancellationToken.None);

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

app.MapGet("/audit/request/{requestId}",
    async Task<IResult> (
        string requestId,
        ClearanceGate.Application.Abstractions.IAuditQueryService service,
        CancellationToken cancellationToken) =>
    {
        var response = await service.GetAuditByRequestIdAsync(requestId, cancellationToken);
        return response is null ? TypedResults.NotFound() : TypedResults.Ok(response);
    });

app.MapGet("/audit/{decisionId}/export",
    async Task<IResult> (
        string decisionId,
        ClearanceGate.Application.Abstractions.IAuditQueryService service,
        CancellationToken cancellationToken) =>
    {
        var response = await service.ExportAuditAsync(decisionId, cancellationToken);
        return response is null ? TypedResults.NotFound() : TypedResults.Ok(response);
    });

app.MapGet("/audit/request/{requestId}/export",
    async Task<IResult> (
        string requestId,
        ClearanceGate.Application.Abstractions.IAuditQueryService service,
        CancellationToken cancellationToken) =>
    {
        var response = await service.ExportAuditByRequestIdAsync(requestId, cancellationToken);
        return response is null ? TypedResults.NotFound() : TypedResults.Ok(response);
    });

app.MapGet("/profiles",
    (ClearanceGate.Profiles.IProfileCatalog catalog) =>
    {
        var response = new ClearanceGate.Contracts.ProfileCatalogResponse(
            catalog.ListProfiles()
                .Select(profile => new ClearanceGate.Contracts.ProfileCatalogItemResponse(
                    profile.Profile,
                    profile.Family,
                    profile.Version,
                    profile.Description,
                    profile.IsLatest))
                .ToArray());

        return TypedResults.Ok(response);
    });

app.MapGet("/profiles/latest/{family}",
    IResult (string family, ClearanceGate.Profiles.IProfileCatalog catalog) =>
    {
        try
        {
            var latest = catalog.GetLatestProfile(family);
            var response = new ClearanceGate.Contracts.LatestProfileResponse(
                latest.Family,
                new ClearanceGate.Contracts.ProfileCatalogItemResponse(
                    latest.Profile,
                    latest.Family,
                    latest.Version,
                    latest.Description,
                    latest.IsLatest));
            return TypedResults.Ok(response);
        }
        catch (KeyNotFoundException)
        {
            return TypedResults.NotFound();
        }
    });

app.Run();

public partial class Program;
