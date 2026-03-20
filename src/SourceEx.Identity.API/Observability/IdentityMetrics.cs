using Prometheus;

namespace SourceEx.Identity.API.Observability;

/// <summary>
/// Prometheus metrics for identity-facing authentication flows.
/// </summary>
public static class IdentityMetrics
{
    public static readonly Counter LoginAttempts = Metrics.CreateCounter(
        "sourceex_identity_login_attempts_total",
        "Total number of identity login attempts grouped by result.",
        new CounterConfiguration
        {
            LabelNames = ["result"]
        });

    public static readonly Counter RegistrationCount = Metrics.CreateCounter(
        "sourceex_identity_registrations_total",
        "Total number of successfully created self-service identity accounts.");

    public static readonly Counter RefreshRequests = Metrics.CreateCounter(
        "sourceex_identity_refresh_requests_total",
        "Total number of refresh token requests grouped by result.",
        new CounterConfiguration
        {
            LabelNames = ["result"]
        });
}
