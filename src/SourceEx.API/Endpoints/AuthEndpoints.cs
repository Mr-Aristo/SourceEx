using System.Security.Claims;
using Asp.Versioning;
using Asp.Versioning.Builder;
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

        group.MapPost("/token", GenerateTokenAsync)
            .MapToApiVersion(new ApiVersion(1, 0))
            .WithName("GenerateToken")
            .WithSummary("Issues a local JWT access token.")
            .WithDescription("Generates a development-friendly JWT token for local testing and integration scenarios.")
            .Produces<AccessTokenResponse>(StatusCodes.Status200OK)
            .ProducesValidationProblem()
            .WithOpenApi();

        group.MapGet("/me", GetCurrentUserAsync)
            .MapToApiVersion(new ApiVersion(1, 0))
            .RequireAuthorization(AuthorizationPolicies.AuthenticatedUser)
            .WithName("GetCurrentUser")
            .WithSummary("Returns the current authenticated user.")
            .WithDescription("Returns the user and department information extracted from the validated JWT access token.")
            .Produces<CurrentUserResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .WithOpenApi();

        return endpoints;
    }

    private static IResult GenerateTokenAsync(GenerateTokenRequest request, JwtTokenIssuer tokenIssuer)
    {
        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(request.UserId))
            errors["userId"] = ["UserId is required."];

        if (string.IsNullOrWhiteSpace(request.DepartmentId))
            errors["departmentId"] = ["DepartmentId is required."];

        if (errors.Count > 0)
            return TypedResults.ValidationProblem(errors);

        var roles = (request.Roles ?? Array.Empty<string>())
            .Where(role => !string.IsNullOrWhiteSpace(role))
            .Select(role => role.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var token = tokenIssuer.CreateAccessToken(request.UserId.Trim(), request.DepartmentId.Trim(), roles);

        return TypedResults.Ok(new AccessTokenResponse(token.AccessToken, token.ExpiresAtUtc));
    }

    private static IResult GetCurrentUserAsync(ClaimsPrincipal user)
    {
        var roles = user.FindAll(ClaimTypes.Role).Select(claim => claim.Value).Distinct().ToArray();

        return TypedResults.Ok(new CurrentUserResponse(
            user.GetRequiredUserId(),
            user.GetRequiredDepartmentId(),
            roles));
    }
}
