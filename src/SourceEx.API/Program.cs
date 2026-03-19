using Asp.Versioning;
using BuildingBlocks.Messaging;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using SourceEx.API.Endpoints;
using SourceEx.API.ExceptionHandling;
using SourceEx.API.RateLimiting;
using SourceEx.API.Security;
using SourceEx.Application;
using SourceEx.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddHttpLogging(_ => { });
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

builder.Services.AddRateLimiter(ApiRateLimiter.Configure);
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddMessageBroker(builder.Configuration);

var app = builder.Build();

app.UseExceptionHandler();
app.UseHttpsRedirection();
app.UseHttpLogging();
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

app.MapOpenApi();
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
