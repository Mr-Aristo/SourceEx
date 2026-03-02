namespace SourceEx.Domain.Exceptions;

/// <summary>
/// Represents errors that occur when a business rule or domain constraint is violated.
/// </summary>
/// <remarks>Use this exception to indicate that an operation failed due to a condition specific to the
/// application's domain logic, rather than a technical or infrastructure error. This exception is typically thrown when
/// input or state does not satisfy business requirements.</remarks>
public class DomainException : Exception
{
    public DomainException(string message) : base(message)
    {
    }
}
