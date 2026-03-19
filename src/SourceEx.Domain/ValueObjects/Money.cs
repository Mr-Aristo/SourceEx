namespace SourceEx.Domain.ValueObjects;

/// <summary>
/// Represents a monetary value with a specific currency.
/// </summary>
/// <remarks>Use this type to encapsulate currency-specific amounts in financial calculations or data models. The
/// currency should follow standard ISO 4217 codes (e.g., "USD", "EUR").</remarks>
/// <param name="Amount">The amount of money represented. Must be greater than zero.</param>
/// <param name="Currency">The ISO currency code associated with the monetary value. Defaults to "USD" if not specified.</param>
public record Money(decimal Amount, string Currency = "USD")
{
    public static Money Of(decimal amount, string currency = "USD")
    {
        if (amount <= 0)
            throw new ArgumentException("Amount must be greater than zero.", nameof(amount));

        if (string.IsNullOrWhiteSpace(currency))
            throw new ArgumentException("Currency is required.", nameof(currency));

        return new Money(amount, currency.ToUpperInvariant());
    }
}
