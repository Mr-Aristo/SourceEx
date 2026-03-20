using MassTransit;
using SourceEx.Contracts.Expenses;

namespace SourceEx.Worker.Audit.Consumers;

/// <summary>
/// Writes audit information for expense approval events.
/// </summary>
public sealed class ExpenseApprovedIntegrationEventConsumer : IConsumer<ExpenseApprovedIntegrationEvent>
{
    private readonly ILogger<ExpenseApprovedIntegrationEventConsumer> _logger;

    public ExpenseApprovedIntegrationEventConsumer(ILogger<ExpenseApprovedIntegrationEventConsumer> logger)
    {
        _logger = logger;
    }

    public Task Consume(ConsumeContext<ExpenseApprovedIntegrationEvent> context)
    {
        _logger.LogInformation(
            "Audit worker recorded expense approval. ExpenseId: {ExpenseId}, ApproverId: {ApproverId}, DepartmentId: {DepartmentId}, MessageId: {MessageId}, CorrelationId: {CorrelationId}.",
            context.Message.ExpenseId,
            context.Message.ApproverId,
            context.Message.ApproverDepartmentId,
            context.MessageId,
            context.CorrelationId);

        return Task.CompletedTask;
    }
}
