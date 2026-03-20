using Prometheus;

namespace SourceEx.Infrastructure.Observability;

/// <summary>
/// Prometheus metrics for the outbox publishing workflow hosted inside the expense API.
/// </summary>
public static class OutboxMetrics
{
    public static readonly Counter PublishedMessages = Metrics.CreateCounter(
        "sourceex_outbox_messages_published_total",
        "Total number of outbox messages published to the message broker.",
        new CounterConfiguration
        {
            LabelNames = ["message_type"]
        });

    public static readonly Counter FailedMessages = Metrics.CreateCounter(
        "sourceex_outbox_messages_failed_total",
        "Total number of outbox message publish attempts that failed.",
        new CounterConfiguration
        {
            LabelNames = ["message_type"]
        });

    public static readonly Gauge PendingMessages = Metrics.CreateGauge(
        "sourceex_outbox_messages_pending",
        "Current number of pending outbox messages waiting to be published.");
}
