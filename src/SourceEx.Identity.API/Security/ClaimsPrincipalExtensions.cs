using System.Security.Claims;
using BuildingBlocks.Security;

namespace SourceEx.Identity.API.Security;

/// <summary>
/// Provides strongly typed helpers for reading required claims.
/// </summary>
public static class ClaimsPrincipalExtensions
{
    public static Guid GetRequiredUserId(this ClaimsPrincipal user)
    {
        var userId = user.FindFirstValue(ClaimNames.UserId)
            ?? throw new UnauthorizedAccessException("The access token is missing the required user_id claim.");

        return Guid.TryParse(userId, out var parsedUserId)
            ? parsedUserId
            : throw new UnauthorizedAccessException("The access token contains an invalid user_id claim.");
    }
}

