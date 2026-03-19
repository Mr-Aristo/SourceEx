using System.Security.Claims;
using BuildingBlocks.Security;

namespace SourceEx.API.Security;

/// <summary>
/// Provides strongly typed helpers for reading required claims.
/// </summary>
public static class ClaimsPrincipalExtensions
{
    public static string GetRequiredUserId(this ClaimsPrincipal user)
    {
        return user.FindFirstValue(ClaimNames.UserId)
            ?? throw new UnauthorizedAccessException("The access token is missing the required user_id claim.");
    }

    public static string GetRequiredDepartmentId(this ClaimsPrincipal user)
    {
        return user.FindFirstValue(ClaimNames.DepartmentId)
            ?? throw new UnauthorizedAccessException("The access token is missing the required department_id claim.");
    }
}
