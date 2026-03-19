using MassTransit;
using SourceEx.Contracts.Expenses;

namespace SourceEx.Worker.Notification.Consumers;

/// <summary>
/// Simulates sending a notification when an expense is approved.
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
            "Notification worker processed approval for expense {ExpenseId} approved by {ApproverId} in department {DepartmentId}.",
            context.Message.ExpenseId,
            context.Message.ApproverId,
            context.Message.ApproverDepartmentId);

        return Task.CompletedTask;
    }
}
