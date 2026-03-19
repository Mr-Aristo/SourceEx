using FluentValidation;

namespace SourceEx.Application.Expenses.Commands.ApproveExpense;

/// <summary>
/// Validates expense approval requests.
/// </summary>
public sealed class ApproveExpenseCommandValidator : AbstractValidator<ApproveExpenseCommand>
{
    public ApproveExpenseCommandValidator()
    {
        RuleFor(command => command.ExpenseId)
            .NotEmpty();

        RuleFor(command => command.ApproverId)
            .NotEmpty()
            .Length(2, 64);

        RuleFor(command => command.ApproverDepartmentId)
            .NotEmpty()
            .Length(2, 64);
    }
}
