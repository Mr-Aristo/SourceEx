namespace SourceEx.Identity.API.Observability;

/// <summary>
/// Defines correlation ID header names and item keys used by the identity host.
/// </summary>
public static class CorrelationIdConstants
{
    public const string HeaderName = "X-Correlation-Id";
    public const string ItemName = "__CorrelationId";
}
