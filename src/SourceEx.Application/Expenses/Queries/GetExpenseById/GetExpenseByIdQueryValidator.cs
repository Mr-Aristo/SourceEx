using FluentValidation;

namespace SourceEx.Application.Expenses.Queries.GetExpenseById;

/// <summary>
/// Validates expense detail queries.
/// </summary>
public sealed class GetExpenseByIdQueryValidator : AbstractValidator<GetExpenseByIdQuery>
{
    public GetExpenseByIdQueryValidator()
    {
        RuleFor(query => query.ExpenseId)
            .NotEmpty();
    }
}
