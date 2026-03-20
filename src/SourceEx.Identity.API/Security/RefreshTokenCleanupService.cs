using Microsoft.EntityFrameworkCore;
using SourceEx.Identity.API.Data.Context;

namespace SourceEx.Identity.API.Security;

/// <summary>
/// Periodically deletes expired or long-revoked refresh tokens.
/// </summary>
public sealed class RefreshTokenCleanupService : BackgroundService
{
    private static readonly TimeSpan CleanupInterval = TimeSpan.FromHours(6);
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly ILogger<RefreshTokenCleanupService> _logger;

    public RefreshTokenCleanupService(
        IServiceScopeFactory serviceScopeFactory,
        ILogger<RefreshTokenCleanupService> logger)
    {
        _serviceScopeFactory = serviceScopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(CleanupInterval);

        while (!stoppingToken.IsCancellationRequested)
        {
            await CleanupAsync(stoppingToken);

            if (!await timer.WaitForNextTickAsync(stoppingToken))
                break;
        }
    }

    private async Task CleanupAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceScopeFactory.CreateScope();

        var dbContext = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        var now = DateTime.UtcNow;
        var revokedCutoff = now - IdentityHardeningDefaults.RevokedRefreshTokenRetention;

        var removedCount = await dbContext.RefreshTokens
            .Where(token =>
                token.ExpiresAtUtc <= now ||
                (token.RevokedAtUtc != null && token.RevokedAtUtc <= revokedCutoff))
            .ExecuteDeleteAsync(cancellationToken);

        if (removedCount > 0)
        {
            _logger.LogInformation(
                "Identity cleanup removed {RemovedCount} expired or revoked refresh tokens.",
                removedCount);
        }
    }
}
