using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Formatting.Compact;

namespace BuildingBlocks.Observability;

/// <summary>
/// Configures a minimal structured logging baseline for SourceEx hosts.
/// </summary>
public static class HostApplicationBuilderExtensions
{
    public static IHostApplicationBuilder AddSourceExStructuredLogging(
        this IHostApplicationBuilder builder,
        string serviceName)
    {
        builder.Logging.ClearProviders();

        builder.Services.AddSerilog((services, loggerConfiguration) =>
        {
            loggerConfiguration
                .ReadFrom.Configuration(builder.Configuration)
                .ReadFrom.Services(services)
                .Enrich.FromLogContext()
                .Enrich.WithProperty("Service", serviceName)
                .Enrich.WithProperty("Environment", builder.Environment.EnvironmentName)
                .WriteTo.Console(new CompactJsonFormatter());
        });

        return builder;
    }
}
