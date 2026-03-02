using SourceEx.Domain.Abstractions;
using SourceEx.Domain.ValueObjects;

namespace SourceEx.Domain.Events;

public record ExpenseCreatedEvent(ExpenseId ExpenseId) : IDomainEvent
{
    public Guid EventId => Guid.NewGuid();
    public DateTime OccurredOn => DateTime.UtcNow;
}

public record ExpenseApprovedEvent(ExpenseId ExpenseId) : IDomainEvent
{
    public Guid EventId => Guid.NewGuid();
    public DateTime OccurredOn => DateTime.UtcNow;
}