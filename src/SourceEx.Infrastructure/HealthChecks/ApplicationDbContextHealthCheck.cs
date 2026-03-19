using Microsoft.Extensions.Diagnostics.HealthChecks;
using SourceEx.Infrastructure.Data.Context;

namespace SourceEx.Infrastructure.HealthChecks;

/// <summary>
/// Verifies that the application database is reachable.
/// </summary>
public sealed class ApplicationDbContextHealthCheck : IHealthCheck
{
    private readonly ApplicationDbContext _dbContext;

    public ApplicationDbContextHealthCheck(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var canConnect = await _dbContext.Database.CanConnectAsync(cancellationToken);

        return canConnect
            ? HealthCheckResult.Healthy("The application database is reachable.")
            : HealthCheckResult.Unhealthy("The application database is not reachable.");
    }
}
