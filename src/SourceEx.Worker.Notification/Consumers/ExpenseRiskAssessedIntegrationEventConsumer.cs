using MassTransit;
using SourceEx.Contracts.Expenses;

namespace SourceEx.Worker.Notification.Consumers;

/// <summary>
/// Simulates notifying reviewers when a risky expense requires manual review.
/// </summary>
public sealed class ExpenseRiskAssessedIntegrationEventConsumer : IConsumer<ExpenseRiskAssessedIntegrationEvent>
{
    private readonly ILogger<ExpenseRiskAssessedIntegrationEventConsumer> _logger;

    public ExpenseRiskAssessedIntegrationEventConsumer(ILogger<ExpenseRiskAssessedIntegrationEventConsumer> logger)
    {
        _logger = logger;
    }

    public Task Consume(ConsumeContext<ExpenseRiskAssessedIntegrationEvent> context)
    {
        if (!context.Message.RequiresManualReview)
            return Task.CompletedTask;

        _logger.LogInformation(
            "Notification worker flagged expense {ExpenseId} for manual review. RiskLevel: {RiskLevel}. Reason: {Reasoning}",
            context.Message.ExpenseId,
            context.Message.RiskLevel,
            context.Message.Reasoning);

        return Task.CompletedTask;
    }
}
