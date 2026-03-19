using Microsoft.Extensions.Diagnostics.HealthChecks;
using SourceEx.Identity.API.Data.Context;

namespace SourceEx.Identity.API.HealthChecks;

/// <summary>
/// Verifies that the identity database is reachable.
/// </summary>
public sealed class IdentityDbContextHealthCheck : IHealthCheck
{
    private readonly IdentityDbContext _dbContext;

    public IdentityDbContextHealthCheck(IdentityDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var canConnect = await _dbContext.Database.CanConnectAsync(cancellationToken);

        return canConnect
            ? HealthCheckResult.Healthy("The identity database is reachable.")
            : HealthCheckResult.Unhealthy("The identity database is not reachable.");
    }
}

