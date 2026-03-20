using SourceEx.Domain.Enums;
using SourceEx.Domain.Events;
using SourceEx.Domain.Exceptions;
using SourceEx.Domain.Models;
using SourceEx.Domain.ValueObjects;

namespace SourceEx.Domain.Tests;

public sealed class ExpenseTests
{
    [Fact]
    public void Create_SetsPendingStatus_AndRaisesCreatedDomainEvent()
    {
        var expense = Expense.Create(
            ExpenseId.Of(Guid.NewGuid()),
            "employee-001",
            "operations",
            Money.Of(1250, "USD"),
            "Hotel reimbursement");

        Assert.Equal(ExpenseStatus.Pending, expense.Status);

        var domainEvent = Assert.Single(expense.DomainEvents);
        var createdEvent = Assert.IsType<ExpenseCreatedDomainEvent>(domainEvent);

        Assert.Equal(expense.Id.Value, createdEvent.ExpenseId);
        Assert.Equal("employee-001", createdEvent.EmployeeId);
        Assert.Equal("operations", createdEvent.DepartmentId);
    }

    [Fact]
    public void Approve_SetsApprovedStatus_AndRaisesApprovedDomainEvent()
    {
        var expense = Expense.Create(
            ExpenseId.Of(Guid.NewGuid()),
            "employee-001",
            "operations",
            Money.Of(800, "EUR"),
            "Flight expense");

        expense.ClearDomainEvents();

        expense.Approve("manager-001", "operations");

        Assert.Equal(ExpenseStatus.Approved, expense.Status);

        var domainEvent = Assert.Single(expense.DomainEvents);
        var approvedEvent = Assert.IsType<ExpenseApprovedDomainEvent>(domainEvent);

        Assert.Equal(expense.Id.Value, approvedEvent.ExpenseId);
        Assert.Equal("manager-001", approvedEvent.ApproverId);
    }

    [Fact]
    public void Approve_Throws_WhenApproverDepartmentDoesNotMatch()
    {
        var expense = Expense.Create(
            ExpenseId.Of(Guid.NewGuid()),
            "employee-001",
            "operations",
            Money.Of(500, "TRY"),
            "Taxi expense");

        var exception = Assert.Throws<DomainException>(() => expense.Approve("manager-001", "finance"));

        Assert.Equal("Only the department that owns the expense can approve it.", exception.Message);
    }
}
