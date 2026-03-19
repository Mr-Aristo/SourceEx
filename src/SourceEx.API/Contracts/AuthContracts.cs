namespace SourceEx.API.Contracts;

/// <summary>
/// Represents the currently authenticated user as seen by the expense API.
/// </summary>
public sealed record CurrentUserResponse(
    string UserId,
    string UserName,
    string DisplayName,
    string Email,
    string DepartmentId,
    string[] Roles);
