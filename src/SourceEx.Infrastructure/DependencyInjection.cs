using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SourceEx.Application.Data;
using SourceEx.Infrastructure.Data.Context;
using SourceEx.Infrastructure.HealthChecks;
using SourceEx.Infrastructure.Outbox;

namespace SourceEx.Infrastructure;

/// <summary>
/// Registers infrastructure services.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Database");

        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException("The 'ConnectionStrings:Database' setting is required.");

        services.AddDbContext<ApplicationDbContext>(options =>
        {
            options.UseNpgsql(connectionString);
        });

        services.AddScoped<IApplicationDbContext>(serviceProvider => serviceProvider.GetRequiredService<ApplicationDbContext>());

        services.AddHostedService<OutboxProcessor>();

        services.AddHealthChecks()
            .AddCheck<ApplicationDbContextHealthCheck>("database", tags: ["ready"]);

        return services;
    }
}
