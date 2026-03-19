using BuildingBlocks.CQRS.Handlers;
using SourceEx.Application.Data;
using SourceEx.Domain.Models;
using SourceEx.Domain.ValueObjects;

namespace SourceEx.Application.Expenses.Commands.CreateExpense;

/// <summary>
/// Handles expense creation commands.
/// </summary>
public sealed class CreateExpenseCommandHandler : ICommandHandler<CreateExpenseCommand, Guid>
{
    private readonly IApplicationDbContext _context;

    public CreateExpenseCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Guid> Handle(CreateExpenseCommand request, CancellationToken cancellationToken)
    {
        var expenseId = ExpenseId.Of(Guid.NewGuid());
        var money = Money.Of(request.Amount, request.Currency);

        var expense = Expense.Create(expenseId, request.EmployeeId, request.DepartmentId, money, request.Description);

        await _context.AddExpenseAsync(expense, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        return expense.Id.Value;
    }
}
