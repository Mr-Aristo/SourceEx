using BuildingBlocks.CQRS.Handlers;
using MediatR;
using SourceEx.Application.Data;
using SourceEx.Domain.ValueObjects;

namespace SourceEx.Application.Expenses.Commands.ApproveExpense;

/// <summary>
/// Handles expense approval commands.
/// </summary>
public sealed class ApproveExpenseCommandHandler : ICommandHandler<ApproveExpenseCommand, Unit>
{
    private readonly IApplicationDbContext _context;

    public ApproveExpenseCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Unit> Handle(ApproveExpenseCommand request, CancellationToken cancellationToken)
    {
        var expenseId = ExpenseId.Of(request.ExpenseId);
        var expense = await _context.GetExpenseByIdAsync(expenseId, cancellationToken);

        if (expense == null)
            throw new KeyNotFoundException($"Expense with ID {expenseId.Value} was not found.");

        expense.Approve(request.ApproverId, request.ApproverDepartmentId);

        await _context.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
