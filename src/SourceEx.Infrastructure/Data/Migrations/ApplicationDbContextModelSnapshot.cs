using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using SourceEx.Domain.Enums;
using SourceEx.Infrastructure.Data.Context;

namespace SourceEx.Infrastructure.Data.Migrations;

[DbContext(typeof(ApplicationDbContext))]
partial class ApplicationDbContextModelSnapshot : ModelSnapshot
{
    protected override void BuildModel(ModelBuilder modelBuilder)
    {
#pragma warning disable 612, 618
        modelBuilder
            .HasAnnotation("ProductVersion", "10.0.3")
            .HasAnnotation("Relational:MaxIdentifierLength", 63);

        NpgsqlModelBuilderExtensions.UseIdentityByDefaultColumns(modelBuilder);

        modelBuilder.Entity("SourceEx.Domain.Models.Expense", entityBuilder =>
        {
            entityBuilder.Property<Guid>("Id")
                .ValueGeneratedNever()
                .HasColumnType("uuid");

            entityBuilder.Property<DateTime>("CreatedAt")
                .HasColumnType("timestamp with time zone");

            entityBuilder.Property<string>("DepartmentId")
                .IsRequired()
                .HasMaxLength(64)
                .HasColumnType("character varying(64)");

            entityBuilder.Property<string>("Description")
                .IsRequired()
                .HasMaxLength(500)
                .HasColumnType("character varying(500)");

            entityBuilder.Property<string>("EmployeeId")
                .IsRequired()
                .HasMaxLength(64)
                .HasColumnType("character varying(64)");

            entityBuilder.Property<ExpenseStatus>("Status")
                .HasColumnType("integer");

            entityBuilder.HasKey("Id");

            entityBuilder.HasIndex("DepartmentId", "Status");

            entityBuilder.ToTable("Expenses");
        });

        modelBuilder.Entity("SourceEx.Infrastructure.Outbox.OutboxMessage", entityBuilder =>
        {
            entityBuilder.Property<Guid>("Id")
                .ValueGeneratedNever()
                .HasColumnType("uuid");

            entityBuilder.Property<string>("Content")
                .IsRequired()
                .HasColumnType("jsonb");

            entityBuilder.Property<string>("Error")
                .HasColumnType("text");

            entityBuilder.Property<DateTime?>("LastAttemptOnUtc")
                .HasColumnType("timestamp with time zone");

            entityBuilder.Property<DateTime>("OccurredOnUtc")
                .HasColumnType("timestamp with time zone");

            entityBuilder.Property<DateTime?>("ProcessedOnUtc")
                .HasColumnType("timestamp with time zone");

            entityBuilder.Property<int>("RetryCount")
                .ValueGeneratedOnAdd()
                .HasColumnType("integer")
                .HasDefaultValue(0);

            entityBuilder.Property<string>("Type")
                .IsRequired()
                .HasMaxLength(512)
                .HasColumnType("character varying(512)");

            entityBuilder.HasKey("Id");

            entityBuilder.HasIndex("ProcessedOnUtc");

            entityBuilder.ToTable("OutboxMessages");
        });

        modelBuilder.Entity("SourceEx.Domain.Models.Expense", entityBuilder =>
        {
            entityBuilder.OwnsOne("SourceEx.Domain.ValueObjects.Money", "Amount", ownedBuilder =>
            {
                ownedBuilder.Property<Guid>("ExpenseId")
                    .HasColumnType("uuid");

                ownedBuilder.Property<decimal>("Amount")
                    .HasColumnType("numeric")
                    .HasColumnName("Amount");

                ownedBuilder.Property<string>("Currency")
                    .IsRequired()
                    .HasMaxLength(3)
                    .HasColumnType("character varying(3)")
                    .HasColumnName("Currency");

                ownedBuilder.HasKey("ExpenseId");

                ownedBuilder.ToTable("Expenses");

                ownedBuilder.WithOwner()
                    .HasForeignKey("ExpenseId");
            });

            entityBuilder.Navigation("Amount")
                .IsRequired();
        });
#pragma warning restore 612, 618
    }
}
