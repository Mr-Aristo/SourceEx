using SourceEx.Domain.Abstractions;
using SourceEx.Domain.Exceptions;
using SourceEx.Domain.Enums;
using SourceEx.Domain.Events;
//using SourceEx.Domain.Exceptions;
using SourceEx.Domain.ValueObjects;

namespace SourceEx.Domain.Models;

public class Expense : Aggregate<ExpenseId>
{
    public ExpenseStatus Status { get; private set; }

    // EF Core'un veritabanından veri okurken nesne üretebilmesi için gereklidir.
    protected Expense() { }

    // Factory Method
    /// <summary>
    /// Creates a new expense record with the specified details and sets its initial status to pending.
    /// </summary>
    /// <remarks>This method also raises a domain event to signal that a new expense has been created. The
    /// returned expense will have its status set to pending by default.</remarks>
    /// <param name="id">The unique identifier for the expense. Must not be null.</param>
    /// <param name="employeeId">The identifier of the employee who submitted the expense. Cannot be null or empty.</param>
    /// <param name="departmentId">The identifier of the department associated with the expense. Cannot be null or empty.</param>
    /// <param name="amount">The monetary amount of the expense. Must represent a valid, non-negative value.</param>
    /// <param name="description">A description of the expense. Can be null or empty if no description is provided.</param>
    /// <returns>An instance of <see cref="Expense"/> initialized with the provided details and a status of pending.</returns>
    public static Expense Create(ExpenseId id, string employeeId, string departmentId, Money amount, string description)
    {
        if (amount <= 0)
            throw new ArgumentException("Harcama tutarı 0'dan büyük olmalıdır.");

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

        expense.AddDomainEvent(new ExpenseCreatedEvent(expense.Id));
        return expense;
    }

    public void Approve(string approverDepartmentId)
    {
        if (Status != ExpenseStatus.Pending)
            throw new DomainException("Only pending expenses can be approved.");

        Status = ExpenseStatus.Approved;

        if (DepartmentId != approverDepartmentId)
            throw new DomainException("Only the department that owns the expense can approve it.");

    // İş Mantığı: Reddetme Aksiyonu
    public void Reject()
    {
        if (Status != ExpenseStatus.Pending)
            throw new InvalidOperationException("Sadece 'Beklemede' olan harcamalar reddedilebilir.");

        Status = ExpenseStatus.Approved;
        AddDomainEvent(new ExpenseApprovedEvent(this.Id));
    }
}
