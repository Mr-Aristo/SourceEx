using SourceEx.Application.Expenses.Commands.ApproveExpense;
using SourceEx.Application.Expenses.Commands.CreateExpense;

namespace SourceEx.Application.Tests;

public sealed class ExpenseCommandValidatorTests
{
    [Fact]
    public void CreateExpenseValidator_ReturnsError_ForInvalidCurrency()
    {
        var validator = new CreateExpenseCommandValidator();

        var result = validator.Validate(new CreateExpenseCommand(
            "employee-001",
            "operations",
            1500,
            "U1D",
            "Conference travel"));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.PropertyName == "Currency");
    }

    [Fact]
    public void ApproveExpenseValidator_ReturnsErrors_ForMissingApproverFields()
    {
        var validator = new ApproveExpenseCommandValidator();

        var result = validator.Validate(new ApproveExpenseCommand(
            Guid.NewGuid(),
            string.Empty,
            string.Empty));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.PropertyName == "ApproverId");
        Assert.Contains(result.Errors, error => error.PropertyName == "ApproverDepartmentId");
    }
}
