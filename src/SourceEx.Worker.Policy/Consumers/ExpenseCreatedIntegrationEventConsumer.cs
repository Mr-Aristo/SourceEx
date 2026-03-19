using MassTransit;
using SourceEx.Contracts.Expenses;
using SourceEx.Integrations.Ollama.Ollama;

namespace SourceEx.Worker.Policy.Consumers;

/// <summary>
/// Evaluates newly created expenses and emits a risk assessment event.
/// </summary>
public sealed class ExpenseCreatedIntegrationEventConsumer : IConsumer<ExpenseCreatedIntegrationEvent>
{
    private readonly IExpenseRiskAssessmentService _riskAssessmentService;
    private readonly ILogger<ExpenseCreatedIntegrationEventConsumer> _logger;

    public ExpenseCreatedIntegrationEventConsumer(
        IExpenseRiskAssessmentService riskAssessmentService,
        ILogger<ExpenseCreatedIntegrationEventConsumer> logger)
    {
        _riskAssessmentService = riskAssessmentService;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<ExpenseCreatedIntegrationEvent> context)
    {
        var assessment = await _riskAssessmentService.AssessAsync(context.Message, context.CancellationToken);

        await context.Publish(
            new ExpenseRiskAssessedIntegrationEvent(
                context.Message.ExpenseId,
                assessment.RiskLevel,
                assessment.RequiresManualReview,
                assessment.ConfidenceScore,
                assessment.Reasoning),
            context.CancellationToken);

        _logger.LogInformation(
            "Policy worker assessed expense {ExpenseId}. RiskLevel: {RiskLevel}, RequiresManualReview: {RequiresManualReview}, ConfidenceScore: {ConfidenceScore}.",
            context.Message.ExpenseId,
            assessment.RiskLevel,
            assessment.RequiresManualReview,
            assessment.ConfidenceScore);
    }
}
