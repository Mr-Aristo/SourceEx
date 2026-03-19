using System.Security.Claims;
using Asp.Versioning;
using Asp.Versioning.Builder;
using BuildingBlocks.Security;
using SourceEx.API.Contracts;
using SourceEx.API.Security;

namespace SourceEx.API.Endpoints;

/// <summary>
/// Maps authentication-related endpoints.
/// </summary>
public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var versionSet = endpoints.NewApiVersionSet()
            .HasApiVersion(new ApiVersion(1, 0))
            .ReportApiVersions()
            .Build();

        var group = endpoints.MapGroup("/api/v{version:apiVersion}/auth")
            .WithApiVersionSet(versionSet)
            .WithGroupName("v1")
            .WithTags("Auth");

        group.MapGet("/me", GetCurrentUserAsync)
            .MapToApiVersion(new ApiVersion(1, 0))
            .RequireAuthorization(AuthorizationPolicies.AuthenticatedUser)
            .WithName("GetCurrentUser")
            .WithSummary("Returns the current authenticated user.")
            .WithDescription("Returns the identity and role information extracted from the validated JWT access token issued by the identity service.")
            .Produces<CurrentUserResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .WithOpenApi();

        return endpoints;
    }

    private static IResult GetCurrentUserAsync(ClaimsPrincipal user)
    {
        var roles = user.FindAll(ClaimTypes.Role).Select(claim => claim.Value).Distinct().ToArray();

        return TypedResults.Ok(new CurrentUserResponse(
            user.GetRequiredUserId(),
            user.Identity?.Name ?? user.GetRequiredUserId(),
            user.FindFirstValue(ClaimNames.DisplayName) ?? user.Identity?.Name ?? user.GetRequiredUserId(),
            user.FindFirstValue(ClaimTypes.Email) ?? string.Empty,
            user.GetRequiredDepartmentId(),
            roles));
    }
}
