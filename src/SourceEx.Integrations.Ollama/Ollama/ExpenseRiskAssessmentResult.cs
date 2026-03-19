namespace SourceEx.Integrations.Ollama.Ollama;

/// <summary>
/// Represents the normalized risk assessment returned by the policy engine.
/// </summary>
public sealed record ExpenseRiskAssessmentResult(
    string RiskLevel,
    bool RequiresManualReview,
    decimal ConfidenceScore,
    string Reasoning);
