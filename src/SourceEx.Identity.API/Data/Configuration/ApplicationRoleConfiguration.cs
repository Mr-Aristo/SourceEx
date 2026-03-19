using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SourceEx.Identity.API.Entities;

namespace SourceEx.Identity.API.Data.Configuration;

/// <summary>
/// Configures the persistence model for application roles.
/// </summary>
public sealed class ApplicationRoleConfiguration : IEntityTypeConfiguration<ApplicationRole>
{
    public void Configure(EntityTypeBuilder<ApplicationRole> builder)
    {
        builder.ToTable("IdentityRoles");

        builder.HasKey(role => role.Id);

        builder.Property(role => role.Name)
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(role => role.NormalizedName)
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(role => role.Description)
            .HasMaxLength(256)
            .IsRequired();

        builder.HasIndex(role => role.NormalizedName)
            .IsUnique();
    }
}

