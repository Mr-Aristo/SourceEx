using FluentValidation;

namespace SourceEx.Application.Expenses.Commands.CreateExpense;

/// <summary>
/// Validates expense creation requests.
/// </summary>
public sealed class CreateExpenseCommandValidator : AbstractValidator<CreateExpenseCommand>
{
    public CreateExpenseCommandValidator()
    {
        RuleFor(command => command.EmployeeId)
            .NotEmpty()
            .Length(2, 64);

        RuleFor(command => command.DepartmentId)
            .NotEmpty()
            .Length(2, 64);

        RuleFor(command => command.Amount)
            .GreaterThan(0);

        RuleFor(command => command.Currency)
            .NotEmpty()
            .Length(3)
            .Must(currency => currency.All(char.IsLetter))
            .WithMessage("Currency must contain exactly 3 alphabetic characters.");

        RuleFor(command => command.Description)
            .NotEmpty()
            .Length(3, 500);
    }
}
