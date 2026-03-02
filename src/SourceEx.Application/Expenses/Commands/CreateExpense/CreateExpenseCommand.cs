namespace SourceEx.Application.Expenses.Commands.CreateExpense;

public record CreateExpenseCommand(
    string EmployeeId,
    string DepartmentId,
    decimal Amount,
    string Currency,
    string Description) : ICommand<Guid>;