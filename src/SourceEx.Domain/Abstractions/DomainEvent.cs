namespace SourceEx.Domain.Abstractions;

/// <summary>
/// Provides a stable base implementation for domain events.
/// </summary>
public abstract record DomainEvent : IDomainEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime OccurredOnUtc { get; init; } = DateTime.UtcNow;
}
