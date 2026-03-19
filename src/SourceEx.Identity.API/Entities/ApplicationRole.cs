namespace SourceEx.Identity.API.Entities;

/// <summary>
/// Represents a platform role that can be assigned to users.
/// </summary>
public sealed class ApplicationRole
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string NormalizedName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    public ICollection<ApplicationUserRole> UserRoles { get; set; } = [];
}

