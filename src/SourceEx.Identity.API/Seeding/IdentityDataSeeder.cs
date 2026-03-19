using BuildingBlocks.Security;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SourceEx.Identity.API.Data.Context;
using SourceEx.Identity.API.Entities;
using SourceEx.Identity.API.Security;

namespace SourceEx.Identity.API.Seeding;

/// <summary>
/// Seeds well-known roles and local bootstrap users for development and integration scenarios.
/// </summary>
public sealed class IdentityDataSeeder
{
    private readonly IdentityDbContext _dbContext;
    private readonly IPasswordHasher<ApplicationUser> _passwordHasher;
    private readonly IOptions<IdentitySeedOptions> _seedOptions;
    private readonly ILogger<IdentityDataSeeder> _logger;

    public IdentityDataSeeder(
        IdentityDbContext dbContext,
        IPasswordHasher<ApplicationUser> passwordHasher,
        IOptions<IdentitySeedOptions> seedOptions,
        ILogger<IdentityDataSeeder> logger)
    {
        _dbContext = dbContext;
        _passwordHasher = passwordHasher;
        _seedOptions = seedOptions;
        _logger = logger;
    }

    public async Task SeedAsync()
    {
        await _dbContext.Database.EnsureCreatedAsync();

        await EnsureRolesAsync();

        if (!_seedOptions.Value.Enabled)
            return;

        await EnsureUserAsync("employee-001", "employee-001@sourceex.local", "Employee One", "operations", [RoleNames.Employee]);
        await EnsureUserAsync("manager-001", "manager-001@sourceex.local", "Manager One", "operations", [RoleNames.Manager]);
        await EnsureUserAsync("finance-001", "finance-001@sourceex.local", "Finance One", "finance", [RoleNames.Finance]);
        await EnsureUserAsync("admin-001", "admin-001@sourceex.local", "Administrator", "operations", [RoleNames.Admin]);
    }

    private async Task EnsureRolesAsync()
    {
        foreach (var roleName in RoleNames.All)
        {
            var normalizedRoleName = roleName.ToUpperInvariant();
            var existingRole = await _dbContext.Roles.FirstOrDefaultAsync(role => role.NormalizedName == normalizedRoleName);

            if (existingRole is not null)
                continue;

            _dbContext.Roles.Add(new ApplicationRole
            {
                Name = roleName,
                NormalizedName = normalizedRoleName,
                Description = $"{roleName} role"
            });
        }

        await _dbContext.SaveChangesAsync();
    }

    private async Task EnsureUserAsync(
        string userName,
        string email,
        string displayName,
        string departmentId,
        IReadOnlyCollection<string> roles)
    {
        var normalizedUserName = userName.ToUpperInvariant();
        var existingUser = await _dbContext.Users
            .Include(user => user.UserRoles)
            .ThenInclude(userRole => userRole.Role)
            .FirstOrDefaultAsync(user => user.NormalizedUserName == normalizedUserName);

        if (existingUser is null)
        {
            existingUser = new ApplicationUser
            {
                UserName = userName,
                NormalizedUserName = normalizedUserName,
                Email = email,
                NormalizedEmail = email.ToUpperInvariant(),
                DisplayName = displayName,
                DepartmentId = departmentId,
                IsActive = true
            };

            existingUser.PasswordHash = _passwordHasher.HashPassword(existingUser, _seedOptions.Value.DemoPassword);
            _dbContext.Users.Add(existingUser);
            await _dbContext.SaveChangesAsync();

            _logger.LogInformation("Seeded identity user {UserName}.", userName);
        }

        var roleEntities = await _dbContext.Roles
            .Where(role => roles.Select(item => item.ToUpperInvariant()).Contains(role.NormalizedName))
            .ToListAsync();

        foreach (var roleEntity in roleEntities)
        {
            var alreadyAssigned = await _dbContext.UserRoles.AnyAsync(userRole =>
                userRole.UserId == existingUser.Id &&
                userRole.RoleId == roleEntity.Id);

            if (alreadyAssigned)
                continue;

            _dbContext.UserRoles.Add(new ApplicationUserRole
            {
                UserId = existingUser.Id,
                RoleId = roleEntity.Id
            });
        }

        await _dbContext.SaveChangesAsync();
    }
}

