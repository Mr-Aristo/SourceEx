namespace SourceEx.Infrastructure.Outbox;

/// <summary>
/// Represents a durable integration event waiting to be published.
/// </summary>
public sealed class OutboxMessage
{
    public Guid Id { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTime OccurredOnUtc { get; set; }
    public DateTime? ProcessedOnUtc { get; set; }
    public DateTime? LastAttemptOnUtc { get; set; }
    public int RetryCount { get; set; }
    public string? Error { get; set; }
}
