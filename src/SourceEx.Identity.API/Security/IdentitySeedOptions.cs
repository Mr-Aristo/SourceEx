using System.ComponentModel.DataAnnotations;

namespace SourceEx.Identity.API.Security;

/// <summary>
/// Represents seed options used for local bootstrap accounts.
/// </summary>
public sealed class IdentitySeedOptions
{
    public const string SectionName = "IdentitySeed";

    public bool Enabled { get; init; } = true;

    [Required]
    [MinLength(8)]
    public string DemoPassword { get; init; } = "Passw0rd!";
}

