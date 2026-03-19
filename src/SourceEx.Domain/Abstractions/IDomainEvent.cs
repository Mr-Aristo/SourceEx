namespace SourceEx.Domain.Abstractions;

/// <summary>
/// Represents a domain event that signals a significant change or occurrence within the domain model.
/// </summary>
/// <remarks>Domain events are used to communicate important state changes across different parts of the
/// application. Implementations of this interface are typically dispatched to event handlers via a mediator or event
/// bus. Domain events should be immutable and describe something that has already happened within the domain.</remarks>
public interface IDomainEvent
{
    Guid EventId { get; }
    DateTime OccurredOnUtc { get; }
}
