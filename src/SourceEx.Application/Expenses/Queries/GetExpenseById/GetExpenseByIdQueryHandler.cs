using BuildingBlocks.CQRS.Handlers;
using SourceEx.Application.Data;
using SourceEx.Domain.ValueObjects;

namespace SourceEx.Application.Expenses.Queries.GetExpenseById;

/// <summary>
/// Handles expense detail queries.
/// </summary>
public sealed class GetExpenseByIdQueryHandler : IQueryHandler<GetExpenseByIdQuery, ExpenseDetailsResponse>
{
    private readonly IApplicationDbContext _context;

    public GetExpenseByIdQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ExpenseDetailsResponse> Handle(GetExpenseByIdQuery request, CancellationToken cancellationToken)
    {
        var expense = await _context.GetExpenseByIdAsync(ExpenseId.Of(request.ExpenseId), cancellationToken);

        if (expense == null)
            throw new KeyNotFoundException($"Expense with ID {request.ExpenseId} was not found.");

        return new ExpenseDetailsResponse(
            expense.Id.Value,
            expense.EmployeeId,
            expense.DepartmentId,
            expense.Amount.Amount,
            expense.Amount.Currency,
            expense.Description,
            expense.Status.ToString(),
            expense.CreatedAt);
    }
}
