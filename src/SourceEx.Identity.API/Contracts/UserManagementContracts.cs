namespace SourceEx.Identity.API.Contracts;

/// <summary>
/// Represents an administrator-driven user creation request.
/// </summary>
public sealed record CreateUserRequest(
    string UserName,
    string Email,
    string Password,
    string DisplayName,
    string DepartmentId,
    string[] Roles);

