using System.Security.Claims;
using System.Text;
using BuildingBlocks.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using SourceEx.Identity.API.Entities;

namespace SourceEx.Identity.API.Security;

/// <summary>
/// Registers identity service security components.
/// </summary>
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddIdentitySecurity(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<JwtOptions>()
            .Bind(configuration.GetSection(JwtOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<IdentitySeedOptions>()
            .Bind(configuration.GetSection(IdentitySeedOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        var jwtOptions = configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
            ?? throw new InvalidOperationException("The JWT configuration section is required.");

        if (string.IsNullOrWhiteSpace(jwtOptions.SigningKey) || jwtOptions.SigningKey.Length < 32)
            throw new InvalidOperationException("The JWT signing key must be at least 32 characters long.");

        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SigningKey));

        services.AddScoped<IPasswordHasher<ApplicationUser>, PasswordHasher<ApplicationUser>>();
        services.AddScoped<JwtTokenIssuer>();
        services.AddScoped<RefreshTokenService>();

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
            .AddPolicy(AuthorizationPolicies.IdentityAdmin, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.RequireClaim(ClaimNames.UserId);
                policy.RequireClaim(ClaimNames.DepartmentId);
                policy.RequireRole(RoleNames.Admin);
            });

        return services;
    }
}

