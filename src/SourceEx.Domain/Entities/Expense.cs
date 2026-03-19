using SourceEx.Domain.Abstractions;
using SourceEx.Domain.Exceptions;
using SourceEx.Domain.Enums;
using SourceEx.Domain.Events;
using SourceEx.Domain.ValueObjects;

namespace SourceEx.Domain.Models;

public class Expense : Aggregate<ExpenseId>
{
    public string EmployeeId { get; private set; } = string.Empty;
    public string DepartmentId { get; private set; } = string.Empty;
    public Money Amount { get; private set; } = default!; 
    public string Description { get; private set; } = string.Empty;
    public ExpenseStatus Status { get; private set; }

    protected Expense() { }

    /// <summary>
    /// Creates a new expense record with the specified details and sets its initial status to pending.
    /// </summary>
    public static Expense Create(ExpenseId id, string employeeId, string departmentId, Money amount, string description)
    {
        if (string.IsNullOrWhiteSpace(employeeId))
            throw new DomainException("EmployeeId is required.");

        if (string.IsNullOrWhiteSpace(departmentId))
            throw new DomainException("DepartmentId is required.");

        if (string.IsNullOrWhiteSpace(description))
            throw new DomainException("Description is required.");

        var expense = new Expense
        {
            Id = id,
            EmployeeId = employeeId,
            DepartmentId = departmentId,
            Amount = amount,
            Description = description,
            CreatedAt = DateTime.UtcNow,
            Status = ExpenseStatus.Pending
        };

        expense.AddDomainEvent(new ExpenseCreatedDomainEvent(
            expense.Id.Value,
            expense.EmployeeId,
            expense.DepartmentId,
            expense.Amount.Amount,
            expense.Amount.Currency,
            expense.Description));

        return expense;
    }

    /// <summary>
    /// Approves a pending expense for the owning department.
    /// </summary>
    public void Approve(string approverId, string approverDepartmentId)
    {
        if (Status != ExpenseStatus.Pending)
            throw new DomainException("Only pending expenses can be approved.");

        if (string.IsNullOrWhiteSpace(approverDepartmentId))
            throw new DomainException("ApproverDepartmentId is required.");

        if (DepartmentId != approverDepartmentId)
            throw new DomainException("Only the department that owns the expense can approve it.");

        if (string.IsNullOrWhiteSpace(approverId))
            throw new DomainException("ApproverId is required.");

        Status = ExpenseStatus.Approved;
        AddDomainEvent(new ExpenseApprovedDomainEvent(Id.Value, approverId, approverDepartmentId));
    }

    /// <summary>
    /// Rejects a pending expense.
    /// </summary>
    public void Reject()
    {
        if (Status != ExpenseStatus.Pending)
            throw new DomainException("Only pending expenses can be rejected.");

        Status = ExpenseStatus.Rejected;
        AddDomainEvent(new ExpenseRejectedDomainEvent(Id.Value));
    }
}
