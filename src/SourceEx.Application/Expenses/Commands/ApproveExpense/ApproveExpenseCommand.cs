using BuildingBlocks.CQRS;

namespace SourceEx.Application.Expenses.Commands.ApproveExpense;

public record ApproveExpenseCommand(
    Guid ExpenseId,
    string ApproverId,
    string ApproverDepartmentId) : ICommand;