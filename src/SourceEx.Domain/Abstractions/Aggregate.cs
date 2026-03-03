namespace SourceEx.Domain.Abstractions;

/// <summary>
/// Represents a domain aggregate root with a unique identifier and domain event management capabilities.
/// </summary>
/// <remarks>Aggregates are the central entry point for modifying related entities and encapsulate domain logic.
/// Domain events added to the aggregate can be used to signal changes or trigger side effects within the domain model.
/// This class is intended to be inherited by concrete aggregate root implementations.</remarks>
/// <typeparam name="TId">The type of the unique identifier for the aggregate root.</typeparam>
public abstract class Aggregate<TId> : Entity<TId>, IAggregateRoot
{
    private readonly List<IDomainEvent> _domainEvents = new();
    public IReadOnlyList<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    /// <summary>
    /// Adds a domain event to the collection of events associated with the current entity.
    /// </summary>
    /// <remarks>Domain events are used to signal significant changes or actions within the entity. Added
    /// events can be processed by external handlers after the entity's state changes.</remarks>
    /// <param name="domainEvent">The domain event to add. Cannot be null.</param>
    public void AddDomainEvent(IDomainEvent domainEvent)
    {
        _domainEvents.Add(domainEvent);
    }

    /// <summary>
    /// Removes all domain events from the current entity.
    /// </summary>
    /// <remarks>Call this method to reset the domain event collection, typically after events have been
    /// dispatched or processed. This method does not notify listeners or perform any additional actions beyond clearing
    /// the events.</remarks>
    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }
}