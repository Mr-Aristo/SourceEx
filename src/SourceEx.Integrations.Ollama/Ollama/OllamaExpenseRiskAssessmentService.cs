using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SourceEx.Contracts.Expenses;

namespace SourceEx.Integrations.Ollama.Ollama;

/// <summary>
/// Uses Ollama to classify the risk of an expense request.
/// </summary>
public sealed class OllamaExpenseRiskAssessmentService : IExpenseRiskAssessmentService
{
    private static readonly JsonObject ResponseSchema = new()
    {
        ["type"] = "object",
        ["properties"] = new JsonObject
        {
            ["riskLevel"] = new JsonObject
            {
                ["type"] = "string",
                ["enum"] = new JsonArray("Low", "Medium", "High")
            },
            ["requiresManualReview"] = new JsonObject
            {
                ["type"] = "boolean"
            },
            ["confidenceScore"] = new JsonObject
            {
                ["type"] = "number"
            },
            ["reasoning"] = new JsonObject
            {
                ["type"] = "string"
            }
        },
        ["required"] = new JsonArray("riskLevel", "requiresManualReview", "confidenceScore", "reasoning")
    };

    private readonly IOllamaApi _ollamaApi;
    private readonly OllamaOptions _options;
    private readonly ILogger<OllamaExpenseRiskAssessmentService> _logger;

    public OllamaExpenseRiskAssessmentService(
        IOllamaApi ollamaApi,
        IOptions<OllamaOptions> options,
        ILogger<OllamaExpenseRiskAssessmentService> logger)
    {
        _ollamaApi = ollamaApi;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<ExpenseRiskAssessmentResult> AssessAsync(
        ExpenseCreatedIntegrationEvent integrationEvent,
        CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled)
            return BuildFallbackAssessment(integrationEvent, "Ollama integration is disabled.");

        try
        {
            var response = await _ollamaApi.ChatAsync(
                new OllamaChatRequest(
                    _options.Model,
                    [
                        new OllamaChatMessage("system", """
You are a financial compliance assistant.
Return only JSON that follows the provided schema.
Assess whether the expense should require manual review.
"""),
                        new OllamaChatMessage("user", BuildPrompt(integrationEvent))
                    ],
                    ResponseSchema,
                    Stream: false,
                    Options: new OllamaRequestOptions(Temperature: 0.1m, NumPredict: 256)),
                cancellationToken);

            var parsed = JsonSerializer.Deserialize<ExpenseRiskAssessmentResult>(
                ExtractJsonPayload(response.Message.Content),
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

            if (parsed is null)
                throw new InvalidOperationException("Ollama returned an empty risk assessment payload.");

            return parsed with
            {
                RiskLevel = NormalizeRiskLevel(parsed.RiskLevel),
                ConfidenceScore = decimal.Clamp(parsed.ConfidenceScore, 0m, 1m),
                Reasoning = string.IsNullOrWhiteSpace(parsed.Reasoning)
                    ? "Ollama returned an empty reasoning field."
                    : parsed.Reasoning.Trim()
            };
        }
        catch (Exception exception)
        {
            _logger.LogWarning(
                exception,
                "Ollama risk assessment failed for expense {ExpenseId}. Falling back to deterministic rules.",
                integrationEvent.ExpenseId);

            return BuildFallbackAssessment(integrationEvent, $"Fallback rules were used because Ollama was unavailable: {exception.Message}");
        }
    }

    private static string BuildPrompt(ExpenseCreatedIntegrationEvent integrationEvent)
    {
        return $$"""
Evaluate the following expense:
- ExpenseId: {{integrationEvent.ExpenseId}}
- EmployeeId: {{integrationEvent.EmployeeId}}
- DepartmentId: {{integrationEvent.DepartmentId}}
- Amount: {{integrationEvent.Amount}} {{integrationEvent.Currency}}
- Description: {{integrationEvent.Description}}

Classify the risk level as Low, Medium, or High.
Set requiresManualReview to true if the expense should be reviewed by a human before downstream actions.
Set confidenceScore between 0 and 1.
Keep reasoning concise and specific.
""";
    }

    private static ExpenseRiskAssessmentResult BuildFallbackAssessment(
        ExpenseCreatedIntegrationEvent integrationEvent,
        string fallbackReason)
    {
        var description = integrationEvent.Description.ToLowerInvariant();
        var suspiciousKeywords = new[] { "cash", "gift", "urgent", "manual", "reimbursement", "advance" };
        var containsKeyword = suspiciousKeywords.Any(description.Contains);

        var riskLevel = integrationEvent.Amount switch
        {
            >= 5000m => "High",
            >= 1500m => "Medium",
            _ when containsKeyword => "Medium",
            _ => "Low"
        };

        var requiresManualReview = riskLevel is "High" || containsKeyword;
        var confidenceScore = riskLevel switch
        {
            "High" => 0.55m,
            "Medium" => 0.45m,
            _ => 0.35m
        };

        return new ExpenseRiskAssessmentResult(
            riskLevel,
            requiresManualReview,
            confidenceScore,
            fallbackReason);
    }

    private static string NormalizeRiskLevel(string riskLevel)
    {
        return riskLevel.Trim().ToLowerInvariant() switch
        {
            "high" => "High",
            "medium" => "Medium",
            _ => "Low"
        };
    }

    private static string ExtractJsonPayload(string content)
    {
        var trimmed = content.Trim();

        if (trimmed.StartsWith("```json", StringComparison.OrdinalIgnoreCase))
        {
            trimmed = trimmed["```json".Length..].Trim();
        }
        else if (trimmed.StartsWith("```", StringComparison.OrdinalIgnoreCase))
        {
            trimmed = trimmed[3..].Trim();
        }

        if (trimmed.EndsWith("```", StringComparison.OrdinalIgnoreCase))
            trimmed = trimmed[..^3].Trim();

        return trimmed;
    }
}
