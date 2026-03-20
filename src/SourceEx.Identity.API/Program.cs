using Asp.Versioning;
using BuildingBlocks.Observability;
using BuildingBlocks.Security;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Prometheus;
using SourceEx.Identity.API.Data.Context;
using SourceEx.Identity.API.Endpoints;
using SourceEx.Identity.API.ExceptionHandling;
using SourceEx.Identity.API.HealthChecks;
using SourceEx.Identity.API.Observability;
using SourceEx.Identity.API.RateLimiting;
using SourceEx.Identity.API.Security;
using SourceEx.Identity.API.Seeding;
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

builder.AddSourceExStructuredLogging("sourceex-identity-api");
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddOpenApi();
builder.Services.AddIdentitySecurity(builder.Configuration);

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

var connectionString = builder.Configuration.GetConnectionString("Database");
if (string.IsNullOrWhiteSpace(connectionString))
    throw new InvalidOperationException("The 'ConnectionStrings:Database' setting is required.");

builder.Services.AddDbContext<IdentityDbContext>(options =>
{
    options.UseNpgsql(connectionString);
});

builder.Services.AddScoped<IdentityDataSeeder>();
builder.Services.AddHostedService<RefreshTokenCleanupService>();
builder.Services.AddRateLimiter(IdentityRateLimiter.Configure);

builder.Services.AddHealthChecks()
    .AddCheck<IdentityDbContextHealthCheck>("identity-database", tags: ["ready"]);

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var seeder = scope.ServiceProvider.GetRequiredService<IdentityDataSeeder>();
    await seeder.SeedAsync();
}

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

app.MapIdentityAuthEndpoints();
app.MapIdentityUserEndpoints();

app.Run();

public partial class Program;
