using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using SourceEx.Identity.API.Entities;

namespace SourceEx.Identity.API.Security;

/// <summary>
/// Creates and verifies refresh tokens.
/// </summary>
public sealed class RefreshTokenService
{
    private readonly JwtOptions _jwtOptions;

    public RefreshTokenService(IOptions<JwtOptions> jwtOptions)
    {
        _jwtOptions = jwtOptions.Value;
    }

    public (string PlainTextToken, RefreshToken RefreshToken) Create(Guid userId)
    {
        var plainTextToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        var refreshToken = new RefreshToken
        {
            UserId = userId,
            TokenHash = ComputeHash(plainTextToken),
            ExpiresAtUtc = DateTime.UtcNow.AddDays(_jwtOptions.RefreshTokenLifetimeDays)
        };

        return (plainTextToken, refreshToken);
    }

    public static string ComputeHash(string plainTextToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(plainTextToken);

        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(plainTextToken)));
    }
}

