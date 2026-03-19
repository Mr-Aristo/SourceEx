using System.Security.Claims;
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
/// Maps authentication and session endpoints for the identity module.
/// </summary>
public static class IdentityAuthEndpoints
{
    public static IEndpointRouteBuilder MapIdentityAuthEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var versionSet = endpoints.NewApiVersionSet()
            .HasApiVersion(new ApiVersion(1, 0))
            .ReportApiVersions()
            .Build();

        var group = endpoints.MapGroup("/api/v{version:apiVersion}/identity/auth")
            .WithApiVersionSet(versionSet)
            .WithGroupName("v1")
            .WithTags("Identity");

        group.MapPost("/register", RegisterAsync)
            .MapToApiVersion(new ApiVersion(1, 0))
            .WithName("RegisterIdentityUser")
            .WithSummary("Registers a self-service employee account.")
            .WithDescription("Creates a new employee account with a hashed password and returns access and refresh tokens.")
            .Produces<AuthTokenResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status409Conflict)
            .WithOpenApi();

        group.MapPost("/login", LoginAsync)
            .MapToApiVersion(new ApiVersion(1, 0))
            .WithName("LoginIdentityUser")
            .WithSummary("Authenticates a user with username or email and password.")
            .WithDescription("Returns a signed JWT access token, a refresh token, and the authenticated identity profile.")
            .Produces<AuthTokenResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesValidationProblem()
            .WithOpenApi();

        group.MapPost("/refresh", RefreshAsync)
            .MapToApiVersion(new ApiVersion(1, 0))
            .WithName("RefreshIdentityToken")
            .WithSummary("Rotates an access token by using a refresh token.")
            .WithDescription("Revokes the previous refresh token, issues a new access token, and returns a new refresh token pair.")
            .Produces<AuthTokenResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesValidationProblem()
            .WithOpenApi();

        group.MapPost("/logout", LogoutAsync)
            .MapToApiVersion(new ApiVersion(1, 0))
            .RequireAuthorization(AuthorizationPolicies.AuthenticatedUser)
            .WithName("LogoutIdentityUser")
            .WithSummary("Revokes a refresh token for the current user.")
            .WithDescription("Marks the submitted refresh token as revoked so it can no longer be used to rotate access tokens.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .WithOpenApi();

        group.MapGet("/me", GetCurrentUserAsync)
            .MapToApiVersion(new ApiVersion(1, 0))
            .RequireAuthorization(AuthorizationPolicies.AuthenticatedUser)
            .WithName("GetCurrentIdentityUser")
            .WithSummary("Returns the current authenticated identity user.")
            .WithDescription("Loads the current user's profile and roles from the identity service database.")
            .Produces<IdentityUserResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .WithOpenApi();

        return endpoints;
    }

    private static async Task<IResult> RegisterAsync(
        RegisterUserRequest request,
        IdentityDbContext dbContext,
        IPasswordHasher<ApplicationUser> passwordHasher,
        JwtTokenIssuer tokenIssuer,
        RefreshTokenService refreshTokenService,
        CancellationToken cancellationToken)
    {
        var validationProblem = ValidateRegistrationRequest(request);
        if (validationProblem is not null)
            return validationProblem;

        var normalizedUserName = request.UserName.Trim().ToUpperInvariant();
        var normalizedEmail = request.Email.Trim().ToUpperInvariant();

        if (await dbContext.Users.AnyAsync(user => user.NormalizedUserName == normalizedUserName, cancellationToken))
            return CreateConflictResult("A user with the same username already exists.");

        if (await dbContext.Users.AnyAsync(user => user.NormalizedEmail == normalizedEmail, cancellationToken))
            return CreateConflictResult("A user with the same email address already exists.");

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

        var employeeRole = await dbContext.Roles.FirstAsync(role => role.NormalizedName == RoleNames.Employee.ToUpperInvariant(), cancellationToken);

        dbContext.Users.Add(user);
        dbContext.UserRoles.Add(new ApplicationUserRole
        {
            User = user,
            Role = employeeRole
        });

        var tokenResult = CreateTokenResponse(user, [RoleNames.Employee], tokenIssuer, refreshTokenService);
        dbContext.RefreshTokens.Add(tokenResult.RefreshTokenEntity);

        await dbContext.SaveChangesAsync(cancellationToken);

        return TypedResults.Created(
            $"/api/v1.0/identity/users/{user.Id}",
            tokenResult.Response);
    }

    private static async Task<IResult> LoginAsync(
        LoginRequest request,
        IdentityDbContext dbContext,
        IPasswordHasher<ApplicationUser> passwordHasher,
        JwtTokenIssuer tokenIssuer,
        RefreshTokenService refreshTokenService,
        CancellationToken cancellationToken)
    {
        var validationProblem = ValidateLoginRequest(request);
        if (validationProblem is not null)
            return validationProblem;

        var normalizedCredential = request.UserNameOrEmail.Trim().ToUpperInvariant();

        var user = await dbContext.Users
            .Include(item => item.UserRoles)
            .ThenInclude(item => item.Role)
            .FirstOrDefaultAsync(item =>
                item.NormalizedUserName == normalizedCredential ||
                item.NormalizedEmail == normalizedCredential,
                cancellationToken);

        if (user is null || !user.IsActive)
            return TypedResults.Unauthorized();

        var verificationResult = passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);
        if (verificationResult == PasswordVerificationResult.Failed)
            return TypedResults.Unauthorized();

        user.LastLoginAtUtc = DateTime.UtcNow;

        var roles = user.UserRoles.Select(userRole => userRole.Role.Name).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        var tokenResult = CreateTokenResponse(user, roles, tokenIssuer, refreshTokenService);

        dbContext.RefreshTokens.Add(tokenResult.RefreshTokenEntity);
        await dbContext.SaveChangesAsync(cancellationToken);

        return TypedResults.Ok(tokenResult.Response);
    }

    private static async Task<IResult> RefreshAsync(
        RefreshTokenRequest request,
        IdentityDbContext dbContext,
        JwtTokenIssuer tokenIssuer,
        RefreshTokenService refreshTokenService,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                ["refreshToken"] = ["RefreshToken is required."]
            });

        var tokenHash = RefreshTokenService.ComputeHash(request.RefreshToken.Trim());

        var storedRefreshToken = await dbContext.RefreshTokens
            .Include(token => token.User)
            .ThenInclude(user => user.UserRoles)
            .ThenInclude(userRole => userRole.Role)
            .FirstOrDefaultAsync(token => token.TokenHash == tokenHash, cancellationToken);

        if (storedRefreshToken is null ||
            storedRefreshToken.RevokedAtUtc is not null ||
            storedRefreshToken.ExpiresAtUtc <= DateTime.UtcNow ||
            !storedRefreshToken.User.IsActive)
        {
            return TypedResults.Unauthorized();
        }

        var roles = storedRefreshToken.User.UserRoles
            .Select(userRole => userRole.Role.Name)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var tokenResult = CreateTokenResponse(storedRefreshToken.User, roles, tokenIssuer, refreshTokenService);
        storedRefreshToken.RevokedAtUtc = DateTime.UtcNow;
        storedRefreshToken.ReplacedByTokenHash = tokenResult.RefreshTokenEntity.TokenHash;

        dbContext.RefreshTokens.Add(tokenResult.RefreshTokenEntity);
        await dbContext.SaveChangesAsync(cancellationToken);

        return TypedResults.Ok(tokenResult.Response);
    }

    private static async Task<IResult> LogoutAsync(
        LogoutRequest request,
        ClaimsPrincipal user,
        IdentityDbContext dbContext,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                ["refreshToken"] = ["RefreshToken is required."]
            });

        var userId = user.GetRequiredUserId();
        var tokenHash = RefreshTokenService.ComputeHash(request.RefreshToken.Trim());

        var storedRefreshToken = await dbContext.RefreshTokens
            .FirstOrDefaultAsync(token =>
                token.UserId == userId &&
                token.TokenHash == tokenHash &&
                token.RevokedAtUtc == null,
                cancellationToken);

        if (storedRefreshToken is null)
            return TypedResults.NoContent();

        storedRefreshToken.RevokedAtUtc = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);

        return TypedResults.NoContent();
    }

    private static async Task<IResult> GetCurrentUserAsync(
        ClaimsPrincipal user,
        IdentityDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var userId = user.GetRequiredUserId();

        var existingUser = await dbContext.Users
            .Include(item => item.UserRoles)
            .ThenInclude(item => item.Role)
            .FirstOrDefaultAsync(item => item.Id == userId, cancellationToken);

        if (existingUser is null)
            throw new KeyNotFoundException($"Identity user with ID {userId} was not found.");

        return TypedResults.Ok(MapUserResponse(existingUser));
    }

    private static IResult? ValidateRegistrationRequest(RegisterUserRequest request)
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

        return errors.Count == 0 ? null : TypedResults.ValidationProblem(errors);
    }

    private static IResult? ValidateLoginRequest(LoginRequest request)
    {
        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(request.UserNameOrEmail))
            errors["userNameOrEmail"] = ["UserNameOrEmail is required."];

        if (string.IsNullOrWhiteSpace(request.Password))
            errors["password"] = ["Password is required."];

        return errors.Count == 0 ? null : TypedResults.ValidationProblem(errors);
    }

    private static IResult CreateConflictResult(string detail)
    {
        return Results.Problem(
            title: "The identity request could not be completed.",
            detail: detail,
            statusCode: StatusCodes.Status409Conflict);
    }

    private static IdentityUserResponse MapUserResponse(ApplicationUser user)
    {
        return new IdentityUserResponse(
            user.Id,
            user.UserName,
            user.Email,
            user.DisplayName,
            user.DepartmentId,
            user.UserRoles
                .Select(userRole => userRole.Role.Name)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray());
    }

    private static (AuthTokenResponse Response, RefreshToken RefreshTokenEntity) CreateTokenResponse(
        ApplicationUser user,
        IReadOnlyCollection<string> roles,
        JwtTokenIssuer tokenIssuer,
        RefreshTokenService refreshTokenService)
    {
        var (accessToken, accessTokenExpiresAtUtc) = tokenIssuer.CreateAccessToken(user, roles);
        var (plainTextRefreshToken, refreshTokenEntity) = refreshTokenService.Create(user.Id);

        return (
            new AuthTokenResponse(
                accessToken,
                accessTokenExpiresAtUtc,
                plainTextRefreshToken,
                refreshTokenEntity.ExpiresAtUtc,
                new IdentityUserResponse(
                    user.Id,
                    user.UserName,
                    user.Email,
                    user.DisplayName,
                    user.DepartmentId,
                    roles.ToArray())),
            refreshTokenEntity);
    }
}

