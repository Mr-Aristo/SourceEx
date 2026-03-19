using MassTransit;
using SourceEx.Contracts.Expenses;

namespace SourceEx.Worker.Audit.Consumers;

/// <summary>
/// Writes audit information for AI-driven risk assessments.
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
        _logger.LogInformation(
            "Audit worker recorded risk assessment for expense {ExpenseId}. RiskLevel: {RiskLevel}, ManualReview: {RequiresManualReview}, ConfidenceScore: {ConfidenceScore}.",
            context.Message.ExpenseId,
            context.Message.RiskLevel,
            context.Message.RequiresManualReview,
            context.Message.ConfidenceScore);

        return Task.CompletedTask;
    }
}
