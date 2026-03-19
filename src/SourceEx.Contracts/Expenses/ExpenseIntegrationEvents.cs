using BuildingBlocks.Messaging;

namespace SourceEx.Contracts.Expenses;

/// <summary>
/// Published when a new expense is created.
/// </summary>
public sealed record ExpenseCreatedIntegrationEvent(
    Guid ExpenseId,
    string EmployeeId,
    string DepartmentId,
    decimal Amount,
    string Currency,
    string Description) : IntegrationEvent;

/// <summary>
/// Published when an expense is approved.
/// </summary>
public sealed record ExpenseApprovedIntegrationEvent(
    Guid ExpenseId,
    string ApproverId,
    string ApproverDepartmentId) : IntegrationEvent;

/// <summary>
/// Published when an expense is rejected.
/// </summary>
public sealed record ExpenseRejectedIntegrationEvent(Guid ExpenseId) : IntegrationEvent;

/// <summary>
/// Published when an expense has been risk-assessed by the policy engine.
/// </summary>
public sealed record ExpenseRiskAssessedIntegrationEvent(
    Guid ExpenseId,
    string RiskLevel,
    bool RequiresManualReview,
    decimal ConfidenceScore,
    string Reasoning) : IntegrationEvent;
