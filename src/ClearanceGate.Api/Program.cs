var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddProblemDetails();

builder.Services.AddSingleton<ClearanceGate.Application.Abstractions.IAuthorizationService, ClearanceGate.Application.Services.AuthorizationService>();
builder.Services.AddSingleton<ClearanceGate.Application.Abstractions.IAcknowledgmentService, ClearanceGate.Application.Services.AcknowledgmentService>();
builder.Services.AddSingleton<ClearanceGate.Application.Abstractions.IAuditQueryService, ClearanceGate.Application.Services.AuditQueryService>();

var app = builder.Build();

app.UseExceptionHandler();

app.MapPost("/authorize", async (
    ClearanceGate.Contracts.AuthorizationRequest request,
    ClearanceGate.Application.Abstractions.IAuthorizationService service,
    CancellationToken cancellationToken) =>
{
    var response = await service.AuthorizeAsync(request, cancellationToken);
    return TypedResults.Ok(response);
});

app.MapPost("/acknowledge", async (
    ClearanceGate.Contracts.AcknowledgmentRequest request,
    ClearanceGate.Application.Abstractions.IAcknowledgmentService service,
    CancellationToken cancellationToken) =>
{
    var response = await service.AcknowledgeAsync(request, cancellationToken);
    return TypedResults.Ok(response);
});

app.MapGet("/audit/{decisionId}", async (
    string decisionId,
    ClearanceGate.Application.Abstractions.IAuditQueryService service,
    CancellationToken cancellationToken) =>
{
    var response = await service.GetAuditAsync(decisionId, cancellationToken);
    return response is null ? TypedResults.NotFound() : TypedResults.Ok(response);
});

app.Run();
