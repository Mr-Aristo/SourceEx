namespace SourceEx.Domain.ValueObjects;

/// <summary>
/// Represents a strongly-typed identifier for an expense.
/// </summary>
public record ExpenseId(Guid Value)
{
    /// <summary>
    /// Creates a new instance of the ExpenseId type using the specified GUID value.
    /// it is a value object that encapsulates a GUID to provide type safety and
    /// domain-specific semantics for identifying expenses within the application.
    /// </summary>
    /// <returns>An ExpenseId instance representing the specified GUID value.</returns>
    public static ExpenseId Of(Guid value)
    {
        if (value == Guid.Empty)
            throw new ArgumentException("ExpenseId cannot be empty.", nameof(value));

        return new ExpenseId(value);
    }
}
