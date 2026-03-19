using SourceEx.Domain.Abstractions;

namespace SourceEx.Domain.Events;

/// <summary>
/// Raised when a new expense is created.
/// </summary>
public sealed record ExpenseCreatedDomainEvent(
    Guid ExpenseId,
    string EmployeeId,
    string DepartmentId,
    decimal Amount,
    string Currency,
    string Description) : DomainEvent;

/// <summary>
/// Raised when an expense is approved.
/// </summary>
public sealed record ExpenseApprovedDomainEvent(
    Guid ExpenseId,
    string ApproverId,
    string ApproverDepartmentId) : DomainEvent;

/// <summary>
/// Raised when an expense is rejected.
/// </summary>
public sealed record ExpenseRejectedDomainEvent(Guid ExpenseId) : DomainEvent;
