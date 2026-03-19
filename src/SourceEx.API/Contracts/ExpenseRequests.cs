namespace SourceEx.API.Contracts;

/// <summary>
/// Represents the payload required to create a new expense.
/// </summary>
public sealed record CreateExpenseRequest(
    decimal Amount,
    string Currency,
    string Description);

/// <summary>
/// Represents the identifier of a newly created expense.
/// </summary>
public sealed record CreatedExpenseResponse(Guid ExpenseId);

/// <summary>
/// Represents the API model returned for an expense.
/// </summary>
public sealed record ExpenseResponse(
    Guid Id,
    string EmployeeId,
    string DepartmentId,
    decimal Amount,
    string Currency,
    string Description,
    string Status,
    DateTime CreatedAt);
