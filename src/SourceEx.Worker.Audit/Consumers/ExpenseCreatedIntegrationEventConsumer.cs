using MassTransit;
using SourceEx.Contracts.Expenses;

namespace SourceEx.Worker.Audit.Consumers;

/// <summary>
/// Writes audit information for expense creation events.
/// </summary>
public sealed class ExpenseCreatedIntegrationEventConsumer : IConsumer<ExpenseCreatedIntegrationEvent>
{
    private readonly ILogger<ExpenseCreatedIntegrationEventConsumer> _logger;

    public ExpenseCreatedIntegrationEventConsumer(ILogger<ExpenseCreatedIntegrationEventConsumer> logger)
    {
        _logger = logger;
    }

    public Task Consume(ConsumeContext<ExpenseCreatedIntegrationEvent> context)
    {
        _logger.LogInformation(
            "Audit worker recorded expense creation. ExpenseId: {ExpenseId}, EmployeeId: {EmployeeId}, DepartmentId: {DepartmentId}, MessageId: {MessageId}, CorrelationId: {CorrelationId}.",
            context.Message.ExpenseId,
            context.Message.EmployeeId,
            context.Message.DepartmentId,
            context.MessageId,
            context.CorrelationId);

        return Task.CompletedTask;
    }
}
