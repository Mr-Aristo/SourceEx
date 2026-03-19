namespace BuildingBlocks.Messaging;

/// <summary>
/// Represents a stable integration event contract shared across process boundaries.
/// </summary>
public abstract record IntegrationEvent
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public DateTime OccurredOnUtc { get; init; } = DateTime.UtcNow;
}
