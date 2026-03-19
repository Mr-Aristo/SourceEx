using Asp.Versioning;
using Asp.Versioning.Builder;
using BuildingBlocks.Security;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SourceEx.Identity.API.Contracts;
using SourceEx.Identity.API.Data.Context;
using SourceEx.Identity.API.Entities;
using SourceEx.Identity.API.Security;

namespace SourceEx.Identity.API.Endpoints;

/// <summary>
/// Maps administrative identity management endpoints.
/// </summary>
public static class IdentityUserEndpoints
{
    public static IEndpointRouteBuilder MapIdentityUserEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var versionSet = endpoints.NewApiVersionSet()
            .HasApiVersion(new ApiVersion(1, 0))
            .ReportApiVersions()
            .Build();

        var group = endpoints.MapGroup("/api/v{version:apiVersion}/identity/users")
            .WithApiVersionSet(versionSet)
            .WithGroupName("v1")
            .WithTags("Identity Administration")
            .RequireAuthorization(AuthorizationPolicies.IdentityAdmin);

        group.MapPost("/", CreateUserAsync)
            .MapToApiVersion(new ApiVersion(1, 0))
            .WithName("CreateIdentityUser")
            .WithSummary("Creates a new identity user with explicit roles.")
            .WithDescription("Allows administrators to create worker, manager, finance, or admin users and assign their initial roles.")
            .Produces<IdentityUserResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .WithOpenApi();

        return endpoints;
    }

    private static async Task<IResult> CreateUserAsync(
        CreateUserRequest request,
        IdentityDbContext dbContext,
        IPasswordHasher<ApplicationUser> passwordHasher,
        CancellationToken cancellationToken)
    {
        var validationProblem = ValidateCreateUserRequest(request);
        if (validationProblem is not null)
            return validationProblem;

        var normalizedUserName = request.UserName.Trim().ToUpperInvariant();
        var normalizedEmail = request.Email.Trim().ToUpperInvariant();

        if (await dbContext.Users.AnyAsync(user => user.NormalizedUserName == normalizedUserName, cancellationToken))
            return CreateConflictResult("A user with the same username already exists.");

        if (await dbContext.Users.AnyAsync(user => user.NormalizedEmail == normalizedEmail, cancellationToken))
            return CreateConflictResult("A user with the same email address already exists.");

        var normalizedRoles = request.Roles
            .Where(role => !string.IsNullOrWhiteSpace(role))
            .Select(role => role.Trim().ToLowerInvariant())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var unknownRoles = normalizedRoles
            .Where(role => !RoleNames.All.Contains(role, StringComparer.OrdinalIgnoreCase))
            .ToArray();

        if (unknownRoles.Length > 0)
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                ["roles"] = [$"Unknown roles: {string.Join(", ", unknownRoles)}."]
            });
        }

        var user = new ApplicationUser
        {
            UserName = request.UserName.Trim(),
            NormalizedUserName = normalizedUserName,
            Email = request.Email.Trim(),
            NormalizedEmail = normalizedEmail,
            DisplayName = request.DisplayName.Trim(),
            DepartmentId = request.DepartmentId.Trim(),
            IsActive = true
        };

        user.PasswordHash = passwordHasher.HashPassword(user, request.Password);

        var roles = await dbContext.Roles
            .Where(role => normalizedRoles.Select(item => item.ToUpperInvariant()).Contains(role.NormalizedName))
            .ToListAsync(cancellationToken);

        dbContext.Users.Add(user);

        foreach (var role in roles)
        {
            dbContext.UserRoles.Add(new ApplicationUserRole
            {
                User = user,
                Role = role
            });
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        var response = new IdentityUserResponse(
            user.Id,
            user.UserName,
            user.Email,
            user.DisplayName,
            user.DepartmentId,
            roles.Select(role => role.Name).ToArray());

        return TypedResults.Created($"/api/v1.0/identity/users/{user.Id}", response);
    }

    private static IResult? ValidateCreateUserRequest(CreateUserRequest request)
    {
        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(request.UserName))
            errors["userName"] = ["UserName is required."];

        if (string.IsNullOrWhiteSpace(request.Email))
            errors["email"] = ["Email is required."];

        if (string.IsNullOrWhiteSpace(request.Password))
            errors["password"] = ["Password is required."];
        else if (request.Password.Length < 8)
            errors["password"] = ["Password must be at least 8 characters long."];

        if (string.IsNullOrWhiteSpace(request.DisplayName))
            errors["displayName"] = ["DisplayName is required."];

        if (string.IsNullOrWhiteSpace(request.DepartmentId))
            errors["departmentId"] = ["DepartmentId is required."];

        if (request.Roles is null || request.Roles.Length == 0)
            errors["roles"] = ["At least one role is required."];

        return errors.Count == 0 ? null : TypedResults.ValidationProblem(errors);
    }

    private static IResult CreateConflictResult(string detail)
    {
        return Results.Problem(
            title: "The identity request could not be completed.",
            detail: detail,
            statusCode: StatusCodes.Status409Conflict);
    }
}

