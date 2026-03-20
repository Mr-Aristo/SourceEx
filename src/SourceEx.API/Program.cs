using Asp.Versioning;
using BuildingBlocks.Messaging;
using BuildingBlocks.Observability;
using BuildingBlocks.Security;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.HttpOverrides;
using Prometheus;
using SourceEx.API.Endpoints;
using SourceEx.API.ExceptionHandling;
using SourceEx.API.Observability;
using SourceEx.API.RateLimiting;
using SourceEx.API.Security;
using SourceEx.Application;
using SourceEx.Infrastructure.Bootstrap;
using SourceEx.Infrastructure;
using Serilog;
using Serilog.Events;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.Configure(options =>
{
    options.ActivityTrackingOptions =
        ActivityTrackingOptions.TraceId |
        ActivityTrackingOptions.SpanId |
        ActivityTrackingOptions.ParentId;
});

builder.AddSourceExStructuredLogging("sourceex-api");
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddOpenApi();
builder.Services.AddApiSecurity(builder.Configuration);

builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true;
    options.ApiVersionReader = ApiVersionReader.Combine(
        new UrlSegmentApiVersionReader(),
        new HeaderApiVersionReader("x-api-version"));
});

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders =
        ForwardedHeaders.XForwardedFor |
        ForwardedHeaders.XForwardedProto |
        ForwardedHeaders.XForwardedHost;

    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

builder.Services.AddRateLimiter(ApiRateLimiter.Configure);
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddMessageBroker(builder.Configuration);

var app = builder.Build();

await app.Services.EnsureSourceExInfrastructureAsync();

app.UseForwardedHeaders();
app.UseExceptionHandler();
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseRouting();
app.UseSerilogRequestLogging(options =>
{
    options.MessageTemplate = "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms.";
    options.GetLevel = (httpContext, _, exception) =>
    {
        if (httpContext.Request.Path.StartsWithSegments("/metrics") ||
            httpContext.Request.Path.StartsWithSegments("/health"))
        {
            return LogEventLevel.Debug;
        }

        if (exception is not null || httpContext.Response.StatusCode >= StatusCodes.Status500InternalServerError)
            return LogEventLevel.Error;

        if (httpContext.Response.StatusCode >= StatusCodes.Status400BadRequest)
            return LogEventLevel.Warning;

        return LogEventLevel.Information;
    };
    options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
    {
        diagnosticContext.Set("RequestHost", httpContext.Request.Host.Value);
        diagnosticContext.Set("RequestScheme", httpContext.Request.Scheme);
        diagnosticContext.Set("RequestProtocol", httpContext.Request.Protocol);
        diagnosticContext.Set("RemoteIpAddress", httpContext.Connection.RemoteIpAddress?.ToString());
        diagnosticContext.Set("TraceIdentifier", httpContext.TraceIdentifier);
        diagnosticContext.Set("EndpointName", httpContext.GetEndpoint()?.DisplayName);

        if (httpContext.Items.TryGetValue(CorrelationIdConstants.ItemName, out var correlationId) &&
            correlationId is string correlationIdValue)
        {
            diagnosticContext.Set("CorrelationId", correlationIdValue);
        }

        var userId = httpContext.User.FindFirst(ClaimNames.UserId)?.Value;
        if (!string.IsNullOrWhiteSpace(userId))
            diagnosticContext.Set("UserId", userId);

        var departmentId = httpContext.User.FindFirst(ClaimNames.DepartmentId)?.Value;
        if (!string.IsNullOrWhiteSpace(departmentId))
            diagnosticContext.Set("DepartmentId", departmentId);
    };
});
app.UseHttpsRedirection();
app.UseHttpMetrics();
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

app.MapOpenApi();
app.MapMetrics("/metrics");
app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = _ => false
});
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = registration => registration.Tags.Contains("ready")
});

app.MapAuthEndpoints();
app.MapExpenseEndpoints();

app.Run();

public partial class Program;
