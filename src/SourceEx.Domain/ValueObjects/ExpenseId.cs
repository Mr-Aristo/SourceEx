namespace SourceEx.Domain.ValueObjects;

/// <summary>
/// Represents a strongly-typed identifier for an expense.
/// </summary>
public record ExpenseId(Guid Value)
{
    public static ExpenseId Of(Guid value) => new(value);
}
