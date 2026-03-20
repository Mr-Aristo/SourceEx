using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SourceEx.Infrastructure.Data.Context;
using Microsoft.EntityFrameworkCore;

namespace SourceEx.Infrastructure.Bootstrap;

/// <summary>
/// Bootstraps infrastructure resources that are required for local and integration environments.
/// </summary>
public static class InfrastructureBootstrapper
{
    public static async Task EnsureSourceExInfrastructureAsync(this IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();

        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>()
            .CreateLogger("InfrastructureBootstrapper");
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        logger.LogInformation("Applying pending migrations for the SourceEx application database.");
        await dbContext.Database.MigrateAsync();
    }
}
