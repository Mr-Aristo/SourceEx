namespace SourceEx.Domain.Abstractions;

/// <summary>
/// Represents a base entity with a unique identifier and creation timestamp.
/// </summary>
/// <remarks>This abstract class provides common properties for entities, including an identifier and the UTC
/// creation time. It is intended to be inherited by domain model classes that require identity and audit
/// information.</remarks>
/// <typeparam name="TId">The type of the unique identifier for the entity.</typeparam>
public abstract class Entity<TId> : IEntity<TId>
{
    public TId Id { get; protected set; } = default!;
    public DateTime CreatedAt { get; protected set; } = DateTime.UtcNow;

    protected Entity() { } 
}
