using SourceEx.Domain.Common;
using SourceEx.Domain.Enums;
using SourceEx.Domain.Events;

namespace SourceEx.Domain.Entities;

public class Expense : AggregareRoot
{
    public Guid Id { get; private set; }
    public string EmployeeId { get; private set; } // Talebi açan kişi
    public string DepartmentId { get; private set; } // IT, HR vb.
    public decimal Amount { get; private set; }
    public string Description { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public ExpenseStatus Status { get; private set; }

    // EF Core'un veritabanından veri okurken nesne üretebilmesi için gereklidir.
    protected Expense() { }

    // Factory Method: Yeni bir harcama yaratmanın TEK yolu.
    public static Expense Create(string employeeId, string departmentId, decimal amount, string description)
    {
        if (amount <= 0)
            throw new ArgumentException("Harcama tutarı 0'dan büyük olmalıdır.");

        var expense = new Expense
        {
            Id = Guid.NewGuid(),
            EmployeeId = employeeId,
            DepartmentId = departmentId,
            Amount = amount,
            Description = description,
            CreatedAt = DateTime.UtcNow,
            Status = ExpenseStatus.Pending
        };

        // Domain Event'i listeye ekliyoruz
        expense.AddDomainEvent(new ExpenseCreatedDomainEvent(expense.Id));

        return expense;
    }

    // İş Mantığı: Onaylama Aksiyonu
    public void Approve()
    {
        if (Status != ExpenseStatus.Pending)
            throw new InvalidOperationException("Sadece 'Beklemede' olan harcamalar onaylanabilir.");

        Status = ExpenseStatus.Approved;

        // Onaylandığı an bu event'i listeye ekliyoruz.
        AddDomainEvent(new ExpenseApprovedDomainEvent(this.Id));
    }

    // İş Mantığı: Reddetme Aksiyonu
    public void Reject()
    {
        if (Status != ExpenseStatus.Pending)
            throw new InvalidOperationException("Sadece 'Beklemede' olan harcamalar reddedilebilir.");

        Status = ExpenseStatus.Rejected;
    }
}
