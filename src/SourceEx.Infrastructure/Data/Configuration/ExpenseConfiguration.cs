using Microsoft.EntityFrameworkCore;
using SourceEx.Domain.Models;
using SourceEx.Domain.ValueObjects;

namespace SourceEx.Infrastructure.Data.Configuration;

/// <summary>
/// Provides the Entity Framework Core configuration for the Expense entity type.
/// </summary>
/// <remarks>This class defines how the Expense entity is mapped to the database schema using the
/// IEntityTypeConfiguration interface. It specifies table name, key and value object conversions, property constraints,
/// and column mappings. Typically used in the OnModelCreating method to apply custom configuration for the Expense
/// entity.</remarks>
public class ExpenseConfiguration : IEntityTypeConfiguration<Expense>
{
    public void Configure(Microsoft.EntityFrameworkCore.Metadata.Builders.EntityTypeBuilder<Expense> builder)
    {
        builder.ToTable("Expenses");

        // ExpenseId (Value Object) transformasyonu - Guid olarak saklanacak
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
               .HasConversion(
                   expenseId => expenseId.Value, // DB'ye yazarken Guid'i al
                   value => ExpenseId.Of(value)); // DB'den okurken ExpenseId objesi yarat

        // Money (Value Object) dönüşümü - "OwnsOne" kullanarak aynı tabloya 2 kolon ekliyoruz
        builder.OwnsOne(x => x.Amount, moneyBuilder =>
        {
            moneyBuilder.Property(m => m.Amount).HasColumnName("Amount").IsRequired();
            moneyBuilder.Property(m => m.Currency).HasColumnName("Currency").HasMaxLength(3).IsRequired();
        });

        builder.Property(x => x.Description).HasMaxLength(500);
        builder.Property(x => x.EmployeeId).IsRequired();
    }
}
