namespace SourceEx.API.Contracts;

/// <summary>
/// Represents the payload required to mint a local access token.
/// </summary>
public sealed record GenerateTokenRequest(
    string UserId,
    string DepartmentId,
    string[] Roles);

/// <summary>
/// Represents an issued bearer token response.
/// </summary>
public sealed record AccessTokenResponse(
    string AccessToken,
    DateTime ExpiresAtUtc,
    string TokenType = "Bearer");

/// <summary>
/// Represents the currently authenticated user.
/// </summary>
public sealed record CurrentUserResponse(
    string UserId,
    string DepartmentId,
    string[] Roles);
