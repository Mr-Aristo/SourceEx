namespace SourceEx.Identity.API.Security;

/// <summary>
/// Centralizes minimum hardening values used by the identity module.
/// </summary>
public static class IdentityHardeningDefaults
{
    public const int MaxFailedAccessAttempts = 5;
    public static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);
    public static readonly TimeSpan RevokedRefreshTokenRetention = TimeSpan.FromDays(7);
}
