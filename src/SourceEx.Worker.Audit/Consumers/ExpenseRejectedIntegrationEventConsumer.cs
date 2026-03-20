using MassTransit;
using SourceEx.Contracts.Expenses;

namespace SourceEx.Worker.Audit.Consumers;

/// <summary>
/// Writes audit information for expense rejection events.
/// </summary>
public sealed class ExpenseRejectedIntegrationEventConsumer : IConsumer<ExpenseRejectedIntegrationEvent>
{
    private readonly ILogger<ExpenseRejectedIntegrationEventConsumer> _logger;

    public ExpenseRejectedIntegrationEventConsumer(ILogger<ExpenseRejectedIntegrationEventConsumer> logger)
    {
        _logger = logger;
    }

    public Task Consume(ConsumeContext<ExpenseRejectedIntegrationEvent> context)
    {
        _logger.LogInformation(
            "Audit worker recorded expense rejection. ExpenseId: {ExpenseId}, MessageId: {MessageId}, CorrelationId: {CorrelationId}.",
            context.Message.ExpenseId,
            context.MessageId,
            context.CorrelationId);

        return Task.CompletedTask;
    }
}
