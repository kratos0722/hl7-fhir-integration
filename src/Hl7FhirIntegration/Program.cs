using Hl7FhirIntegration.Auth;
using Hl7FhirIntegration.Fhir;
using Hl7FhirIntegration.Hl7;
using Hl7FhirIntegration.Infrastructure;
using Hl7FhirIntegration.Services;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<IPatientRepository, InMemoryPatientRepository>();
builder.Services.AddSingleton<IAuditRepository, InMemoryAuditRepository>();
builder.Services.AddSingleton<PatientService>();

var app = builder.Build();

const string FhirContentType = "application/fhir+json";

static string Actor(HttpRequest req) =>
    req.Headers.TryGetValue("X-Actor", out var a) && a.Count > 0 ? a.ToString() : "unknown";

app.MapGet("/health", () => Results.Ok(new { status = "UP" }));

app.MapPost("/api/hl7", async (HttpRequest req, PatientService svc) =>
{
    using var reader = new StreamReader(req.Body);
    var body = await reader.ReadToEndAsync();
    try
    {
        var patient = svc.IngestHl7(body, Actor(req));
        return Results.Text(FhirPatientMapper.ToFhirJson(patient), FhirContentType, statusCode: 201);
    }
    catch (Hl7ParseException ex)
    {
        return Results.Problem(detail: ex.Message, statusCode: 400, title: "Invalid HL7 message");
    }
}).AddEndpointFilter(new RoleEndpointFilter("patient:write"));

app.MapPost("/fhir/Patient", async (HttpRequest req, PatientService svc) =>
{
    using var reader = new StreamReader(req.Body);
    var body = await reader.ReadToEndAsync();
    try
    {
        var patient = svc.IngestFhir(body, Actor(req));
        return Results.Text(FhirPatientMapper.ToFhirJson(patient), FhirContentType, statusCode: 201);
    }
    catch (FhirMappingException ex)
    {
        return Results.Problem(detail: ex.Message, statusCode: 400, title: "Invalid FHIR resource");
    }
}).AddEndpointFilter(new RoleEndpointFilter("patient:write"));

app.MapGet("/fhir/Patient/{mrn}", (string mrn, HttpRequest req, PatientService svc) =>
{
    try
    {
        var patient = svc.Get(mrn, Actor(req));
        return Results.Text(FhirPatientMapper.ToFhirJson(patient), FhirContentType);
    }
    catch (PatientNotFoundException ex)
    {
        return Results.Problem(detail: ex.Message, statusCode: 404, title: "Patient not found");
    }
}).AddEndpointFilter(new RoleEndpointFilter("patient:read"));

app.MapGet("/api/audit", (PatientService svc) => Results.Ok(svc.AuditTrail()))
    .AddEndpointFilter(new RoleEndpointFilter("audit:read"));

app.Run();

/// <summary>Applies the pure <see cref="Authorization"/> policy to a request's X-Role header.</summary>
internal sealed class RoleEndpointFilter(string permission) : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var role = context.HttpContext.Request.Headers["X-Role"].ToString();
        return Authorization.Check(role, permission) switch
        {
            AuthDecision.Allowed => await next(context),
            AuthDecision.Unauthenticated => Results.Problem(statusCode: 401, title: "Authentication required"),
            _ => Results.Problem(statusCode: 403, title: "Forbidden"),
        };
    }
}
