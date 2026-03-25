using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SourceEx.Identity.API.Data.Context;

namespace SourceEx.Identity.API.Data.Migrations;

[DbContext(typeof(IdentityDbContext))]
[Migration("202603200001_InitialIdentitySchema")]
/// <inheritdoc />
public partial class InitialIdentitySchema : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "IdentityRoles",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                Name = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                NormalizedName = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                Description = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_IdentityRoles", item => item.Id);
            });

        migrationBuilder.CreateTable(
            name: "IdentityUsers",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                UserName = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                NormalizedUserName = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                Email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                NormalizedEmail = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                DisplayName = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                DepartmentId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                PasswordHash = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: false),
                IsActive = table.Column<bool>(type: "boolean", nullable: false),
                AccessFailedCount = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                LockoutEndUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                LastLoginAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_IdentityUsers", item => item.Id);
            });

        migrationBuilder.CreateTable(
            name: "IdentityRefreshTokens",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                UserId = table.Column<Guid>(type: "uuid", nullable: false),
                TokenHash = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                ExpiresAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                RevokedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                ReplacedByTokenHash = table.Column<string>(type: "text", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_IdentityRefreshTokens", item => item.Id);
                table.ForeignKey(
                    name: "FK_IdentityRefreshTokens_IdentityUsers_UserId",
                    column: item => item.UserId,
                    principalTable: "IdentityUsers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "IdentityUserRoles",
            columns: table => new
            {
                UserId = table.Column<Guid>(type: "uuid", nullable: false),
                RoleId = table.Column<Guid>(type: "uuid", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_IdentityUserRoles", item => new { item.UserId, item.RoleId });
                table.ForeignKey(
                    name: "FK_IdentityUserRoles_IdentityRoles_RoleId",
                    column: item => item.RoleId,
                    principalTable: "IdentityRoles",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_IdentityUserRoles_IdentityUsers_UserId",
                    column: item => item.UserId,
                    principalTable: "IdentityUsers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_IdentityRefreshTokens_ExpiresAtUtc",
            table: "IdentityRefreshTokens",
            column: "ExpiresAtUtc");

        migrationBuilder.CreateIndex(
            name: "IX_IdentityRefreshTokens_RevokedAtUtc",
            table: "IdentityRefreshTokens",
            column: "RevokedAtUtc");

        migrationBuilder.CreateIndex(
            name: "IX_IdentityRefreshTokens_TokenHash",
            table: "IdentityRefreshTokens",
            column: "TokenHash",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_IdentityRefreshTokens_UserId",
            table: "IdentityRefreshTokens",
            column: "UserId");

        migrationBuilder.CreateIndex(
            name: "IX_IdentityRoles_NormalizedName",
            table: "IdentityRoles",
            column: "NormalizedName",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_IdentityUserRoles_RoleId",
            table: "IdentityUserRoles",
            column: "RoleId");

        migrationBuilder.CreateIndex(
            name: "IX_IdentityUsers_NormalizedEmail",
            table: "IdentityUsers",
            column: "NormalizedEmail",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_IdentityUsers_NormalizedUserName",
            table: "IdentityUsers",
            column: "NormalizedUserName",
            unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "IdentityRefreshTokens");

        migrationBuilder.DropTable(
            name: "IdentityUserRoles");

        migrationBuilder.DropTable(
            name: "IdentityRoles");

        migrationBuilder.DropTable(
            name: "IdentityUsers");
    }
}
