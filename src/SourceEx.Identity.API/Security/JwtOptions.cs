using System.ComponentModel.DataAnnotations;

namespace SourceEx.Identity.API.Security;

/// <summary>
/// Represents JWT configuration values for the identity service.
/// </summary>
public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    [Required]
    public string Issuer { get; init; } = string.Empty;

    [Required]
    public string Audience { get; init; } = string.Empty;

    [Required]
    [MinLength(32)]
    public string SigningKey { get; init; } = string.Empty;

    [Range(1, 1440)]
    public int AccessTokenLifetimeMinutes { get; init; } = 60;

    [Range(1, 365)]
    public int RefreshTokenLifetimeDays { get; init; } = 14;
}

