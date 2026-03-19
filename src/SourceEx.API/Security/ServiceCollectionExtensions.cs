using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace SourceEx.API.Security;

/// <summary>
/// Registers API authentication and authorization services.
/// </summary>
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApiSecurity(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<JwtOptions>()
            .Bind(configuration.GetSection(JwtOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        var jwtOptions = configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
            ?? throw new InvalidOperationException("The JWT configuration section is required.");

        if (string.IsNullOrWhiteSpace(jwtOptions.SigningKey) || jwtOptions.SigningKey.Length < 32)
            throw new InvalidOperationException("The JWT signing key must be at least 32 characters long.");

        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SigningKey));

        services.AddSingleton<JwtTokenIssuer>();

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.MapInboundClaims = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtOptions.Issuer,
                    ValidAudience = jwtOptions.Audience,
                    IssuerSigningKey = signingKey,
                    NameClaimType = ClaimNames.UserId,
                    RoleClaimType = ClaimTypes.Role,
                    ClockSkew = TimeSpan.FromMinutes(1)
                };
            });

        services.AddAuthorizationBuilder()
            .AddPolicy(AuthorizationPolicies.AuthenticatedUser, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.RequireClaim(ClaimNames.UserId);
                policy.RequireClaim(ClaimNames.DepartmentId);
            })
            .AddPolicy(AuthorizationPolicies.ExpenseApprover, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.RequireClaim(ClaimNames.UserId);
                policy.RequireClaim(ClaimNames.DepartmentId);
                policy.RequireRole("manager", "finance", "admin");
            });

        return services;
    }
}
