namespace SourceEx.Identity.API.Contracts;

/// <summary>
/// Represents the payload required to register a self-service employee account.
/// </summary>
public sealed record RegisterUserRequest(
    string UserName,
    string Email,
    string Password,
    string DisplayName,
    string DepartmentId);

/// <summary>
/// Represents the payload required to authenticate a user.
/// </summary>
public sealed record LoginRequest(
    string UserNameOrEmail,
    string Password);

/// <summary>
/// Represents the payload required to rotate an access token.
/// </summary>
public sealed record RefreshTokenRequest(string RefreshToken);

/// <summary>
/// Represents the payload required to revoke a refresh token.
/// </summary>
public sealed record LogoutRequest(string RefreshToken);

/// <summary>
/// Represents an identity profile returned by the identity service.
/// </summary>
public sealed record IdentityUserResponse(
    Guid UserId,
    string UserName,
    string Email,
    string DisplayName,
    string DepartmentId,
    string[] Roles);

/// <summary>
/// Represents a token pair issued by the identity service.
/// </summary>
public sealed record AuthTokenResponse(
    string AccessToken,
    DateTime AccessTokenExpiresAtUtc,
    string RefreshToken,
    DateTime RefreshTokenExpiresAtUtc,
    IdentityUserResponse User,
    string TokenType = "Bearer");

