namespace SourceEx.Identity.API.Entities;

/// <summary>
/// Represents the many-to-many join between users and roles.
/// </summary>
public sealed class ApplicationUserRole
{
    public Guid UserId { get; set; }
    public Guid RoleId { get; set; }

    public ApplicationUser User { get; set; } = default!;
    public ApplicationRole Role { get; set; } = default!;
}

