using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace SourceEx.API.Security;

/// <summary>
/// Issues local JWT access tokens for development and integration testing.
/// </summary>
public sealed class JwtTokenIssuer
{
    private readonly JwtOptions _options;

    public JwtTokenIssuer(IOptions<JwtOptions> options)
    {
        _options = options.Value;
    }

    public (string AccessToken, DateTime ExpiresAtUtc) CreateAccessToken(
        string userId,
        string departmentId,
        IEnumerable<string> roles)
    {
        var now = DateTime.UtcNow;
        var expiresAtUtc = now.AddMinutes(_options.AccessTokenLifetimeMinutes);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId),
            new(ClaimNames.UserId, userId),
            new(ClaimNames.DepartmentId, departmentId),
            new(ClaimTypes.Name, userId)
        };

        claims.AddRange(
            roles
                .Where(role => !string.IsNullOrWhiteSpace(role))
                .Select(role => new Claim(ClaimTypes.Role, role.Trim())));

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Issuer = _options.Issuer,
            Audience = _options.Audience,
            Expires = expiresAtUtc,
            NotBefore = now,
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SigningKey)),
                SecurityAlgorithms.HmacSha256)
        };

        var handler = new JwtSecurityTokenHandler();
        var token = handler.CreateToken(tokenDescriptor);

        return (handler.WriteToken(token), expiresAtUtc);
    }
}
