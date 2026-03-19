namespace SourceEx.Application.Expenses.Queries.GetExpenseById;

/// <summary>
/// Retrieves a single expense by identifier.
/// </summary>
public sealed record GetExpenseByIdQuery(Guid ExpenseId) : IQuery<ExpenseDetailsResponse>;
