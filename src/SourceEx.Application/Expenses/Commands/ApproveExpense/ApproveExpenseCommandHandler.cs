using BuildingBlocks.CQRS.Handlers;
using MediatR;
using SourceEx.Application.Data;
using SourceEx.Domain.ValueObjects;


namespace SourceEx.Application.Expenses.Commands.ApproveExpense;

internal class ApproveExpenseCommandHandler
: ICommandHandler<ApproveExpenseCommand, Unit>
{
    private readonly IApplicationDbContext _context;

    public ApproveExpenseCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Unit> Handle(ApproveExpenseCommand request, CancellationToken cancellationToken)
    {
        var expenseId = ExpenseId.Of(request.ExpenseId);

        var expense = await _context.Expenses
            .FirstOrDefaultAsync(e => e.Id == expenseId, cancellationToken);

        if (expense == null)
            throw new Exception($"Expense with ID {expenseId.Value} not found.");


        expense.Approve(request.ApproverDepartmentId);

        await _context.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}