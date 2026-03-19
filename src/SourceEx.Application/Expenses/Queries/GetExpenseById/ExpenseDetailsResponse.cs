namespace SourceEx.Application.Expenses.Queries.GetExpenseById;

/// <summary>
/// Represents expense details returned to read operations.
/// </summary>
public sealed record ExpenseDetailsResponse(
    Guid Id,
    string EmployeeId,
    string DepartmentId,
    decimal Amount,
    string Currency,
    string Description,
    string Status,
    DateTime CreatedAt);
