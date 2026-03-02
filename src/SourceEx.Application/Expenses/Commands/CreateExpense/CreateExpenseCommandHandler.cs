using BuildingBlocks.CQRS.Handlers;
using SourceEx.Application.Data;
using SourceEx.Domain.Models;
using SourceEx.Domain.ValueObjects;
using System;
using System.Collections.Generic;
using System.Text;

namespace SourceEx.Application.Expenses.Commands.CreateExpense;

public class CreateExpenseCommandHandler : ICommandHandler<CreateExpenseCommand, Guid>
{
    public readonly IApplicationDbContext _context;

    public CreateExpenseCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Guid> Handle(CreateExpenseCommand request, CancellationToken cancellationToken)
    {
        var expenseId = ExpenseId.Of(Guid.NewGuid());
        var money = Money.Of(request.Amount, request.Currency);

        var expense = Expense.Create(expenseId, request.EmployeeId, request.DepartmentId, money, request.Description);

        //todo; .Add object error 
        //_context.Expenses.Add(expense);
        await _context.SaveChangesAsync(cancellationToken);

        return expense.Id.Value;
    }
}
