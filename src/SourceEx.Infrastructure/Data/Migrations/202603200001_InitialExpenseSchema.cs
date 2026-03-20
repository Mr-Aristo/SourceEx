using Microsoft.EntityFrameworkCore.Migrations;

namespace SourceEx.Infrastructure.Data.Migrations;

/// <inheritdoc />
public partial class InitialExpenseSchema : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Expenses",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                EmployeeId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                DepartmentId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                Amount = table.Column<decimal>(type: "numeric", nullable: false),
                Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                Status = table.Column<int>(type: "integer", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Expenses", item => item.Id);
            });

        migrationBuilder.CreateTable(
            name: "OutboxMessages",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                Type = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                Content = table.Column<string>(type: "jsonb", nullable: false),
                OccurredOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                ProcessedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                LastAttemptOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                RetryCount = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                Error = table.Column<string>(type: "text", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_OutboxMessages", item => item.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_Expenses_DepartmentId_Status",
            table: "Expenses",
            columns: new[] { "DepartmentId", "Status" });

        migrationBuilder.CreateIndex(
            name: "IX_OutboxMessages_ProcessedOnUtc",
            table: "OutboxMessages",
            column: "ProcessedOnUtc");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "Expenses");

        migrationBuilder.DropTable(
            name: "OutboxMessages");
    }
}
