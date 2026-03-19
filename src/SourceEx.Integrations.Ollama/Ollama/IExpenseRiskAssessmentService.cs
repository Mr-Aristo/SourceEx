using SourceEx.Contracts.Expenses;

namespace SourceEx.Integrations.Ollama.Ollama;

/// <summary>
/// Evaluates expense events and produces a risk assessment.
/// </summary>
public interface IExpenseRiskAssessmentService
{
    Task<ExpenseRiskAssessmentResult> AssessAsync(
        ExpenseCreatedIntegrationEvent integrationEvent,
        CancellationToken cancellationToken = default);
}
