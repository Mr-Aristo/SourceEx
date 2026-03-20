using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using SourceEx.Identity.API.Data.Context;

namespace SourceEx.Identity.API.Data.Migrations;

[DbContext(typeof(IdentityDbContext))]
partial class IdentityDbContextModelSnapshot : ModelSnapshot
{
    protected override void BuildModel(ModelBuilder modelBuilder)
    {
#pragma warning disable 612, 618
        modelBuilder
            .HasAnnotation("ProductVersion", "10.0.3")
            .HasAnnotation("Relational:MaxIdentifierLength", 63);

        NpgsqlModelBuilderExtensions.UseIdentityByDefaultColumns(modelBuilder);

        modelBuilder.Entity("SourceEx.Identity.API.Entities.ApplicationRole", entityBuilder =>
        {
            entityBuilder.Property<Guid>("Id")
                .ValueGeneratedNever()
                .HasColumnType("uuid");

            entityBuilder.Property<string>("Description")
                .IsRequired()
                .HasMaxLength(256)
                .HasColumnType("character varying(256)");

            entityBuilder.Property<string>("Name")
                .IsRequired()
                .HasMaxLength(64)
                .HasColumnType("character varying(64)");

            entityBuilder.Property<string>("NormalizedName")
                .IsRequired()
                .HasMaxLength(64)
                .HasColumnType("character varying(64)");

            entityBuilder.HasKey("Id");

            entityBuilder.HasIndex("NormalizedName")
                .IsUnique();

            entityBuilder.ToTable("IdentityRoles");
        });

        modelBuilder.Entity("SourceEx.Identity.API.Entities.ApplicationUser", entityBuilder =>
        {
            entityBuilder.Property<Guid>("Id")
                .ValueGeneratedNever()
                .HasColumnType("uuid");

            entityBuilder.Property<int>("AccessFailedCount")
                .ValueGeneratedOnAdd()
                .HasColumnType("integer")
                .HasDefaultValue(0);

            entityBuilder.Property<DateTime>("CreatedAtUtc")
                .HasColumnType("timestamp with time zone");

            entityBuilder.Property<string>("DepartmentId")
                .IsRequired()
                .HasMaxLength(64)
                .HasColumnType("character varying(64)");

            entityBuilder.Property<string>("DisplayName")
                .IsRequired()
                .HasMaxLength(128)
                .HasColumnType("character varying(128)");

            entityBuilder.Property<string>("Email")
                .IsRequired()
                .HasMaxLength(256)
                .HasColumnType("character varying(256)");

            entityBuilder.Property<bool>("IsActive")
                .HasColumnType("boolean");

            entityBuilder.Property<DateTime?>("LastLoginAtUtc")
                .HasColumnType("timestamp with time zone");

            entityBuilder.Property<DateTime?>("LockoutEndUtc")
                .HasColumnType("timestamp with time zone");

            entityBuilder.Property<string>("NormalizedEmail")
                .IsRequired()
                .HasMaxLength(256)
                .HasColumnType("character varying(256)");

            entityBuilder.Property<string>("NormalizedUserName")
                .IsRequired()
                .HasMaxLength(64)
                .HasColumnType("character varying(64)");

            entityBuilder.Property<string>("PasswordHash")
                .IsRequired()
                .HasMaxLength(1024)
                .HasColumnType("character varying(1024)");

            entityBuilder.Property<string>("UserName")
                .IsRequired()
                .HasMaxLength(64)
                .HasColumnType("character varying(64)");

            entityBuilder.HasKey("Id");

            entityBuilder.HasIndex("NormalizedEmail")
                .IsUnique();

            entityBuilder.HasIndex("NormalizedUserName")
                .IsUnique();

            entityBuilder.ToTable("IdentityUsers");
        });

        modelBuilder.Entity("SourceEx.Identity.API.Entities.ApplicationUserRole", entityBuilder =>
        {
            entityBuilder.Property<Guid>("UserId")
                .HasColumnType("uuid");

            entityBuilder.Property<Guid>("RoleId")
                .HasColumnType("uuid");

            entityBuilder.HasKey("UserId", "RoleId");

            entityBuilder.HasIndex("RoleId");

            entityBuilder.ToTable("IdentityUserRoles");
        });

        modelBuilder.Entity("SourceEx.Identity.API.Entities.RefreshToken", entityBuilder =>
        {
            entityBuilder.Property<Guid>("Id")
                .ValueGeneratedNever()
                .HasColumnType("uuid");

            entityBuilder.Property<DateTime>("CreatedAtUtc")
                .HasColumnType("timestamp with time zone");

            entityBuilder.Property<DateTime>("ExpiresAtUtc")
                .HasColumnType("timestamp with time zone");

            entityBuilder.Property<string?>("ReplacedByTokenHash")
                .HasColumnType("text");

            entityBuilder.Property<DateTime?>("RevokedAtUtc")
                .HasColumnType("timestamp with time zone");

            entityBuilder.Property<string>("TokenHash")
                .IsRequired()
                .HasMaxLength(256)
                .HasColumnType("character varying(256)");

            entityBuilder.Property<Guid>("UserId")
                .HasColumnType("uuid");

            entityBuilder.HasKey("Id");

            entityBuilder.HasIndex("ExpiresAtUtc");

            entityBuilder.HasIndex("RevokedAtUtc");

            entityBuilder.HasIndex("TokenHash")
                .IsUnique();

            entityBuilder.HasIndex("UserId");

            entityBuilder.ToTable("IdentityRefreshTokens");
        });

        modelBuilder.Entity("SourceEx.Identity.API.Entities.ApplicationUserRole", entityBuilder =>
        {
            entityBuilder.HasOne("SourceEx.Identity.API.Entities.ApplicationRole", "Role")
                .WithMany("UserRoles")
                .HasForeignKey("RoleId")
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired();

            entityBuilder.HasOne("SourceEx.Identity.API.Entities.ApplicationUser", "User")
                .WithMany("UserRoles")
                .HasForeignKey("UserId")
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired();

            entityBuilder.Navigation("Role");

            entityBuilder.Navigation("User");
        });

        modelBuilder.Entity("SourceEx.Identity.API.Entities.RefreshToken", entityBuilder =>
        {
            entityBuilder.HasOne("SourceEx.Identity.API.Entities.ApplicationUser", "User")
                .WithMany("RefreshTokens")
                .HasForeignKey("UserId")
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired();

            entityBuilder.Navigation("User");
        });

        modelBuilder.Entity("SourceEx.Identity.API.Entities.ApplicationRole", entityBuilder =>
        {
            entityBuilder.Navigation("UserRoles");
        });

        modelBuilder.Entity("SourceEx.Identity.API.Entities.ApplicationUser", entityBuilder =>
        {
            entityBuilder.Navigation("RefreshTokens");

            entityBuilder.Navigation("UserRoles");
        });
#pragma warning restore 612, 618
    }
}
