using Microsoft.EntityFrameworkCore;
using SourceEx.Application.Data;
using BuildingBlocks.Messaging;
using SourceEx.Contracts.Expenses;
using SourceEx.Domain.Abstractions;
using SourceEx.Domain.Events;
using SourceEx.Domain.Models;
using SourceEx.Domain.ValueObjects;
using SourceEx.Infrastructure.Outbox;
using System.Text.Json;

namespace SourceEx.Infrastructure.Data.Context;

public class ApplicationDbContext : DbContext, IApplicationDbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options ): base(options)
    {}

    public DbSet<Expense> Expenses => Set<Expense>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    /// <summary>
    /// Adds a new expense aggregate to the current unit of work.
    /// </summary>
    public Task AddExpenseAsync(Expense expense, CancellationToken cancellationToken = default)
    {
        Expenses.Add(expense);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Retrieves an expense aggregate by identifier.
    /// </summary>
    public Task<Expense?> GetExpenseByIdAsync(ExpenseId expenseId, CancellationToken cancellationToken = default)
    {
        return Expenses.FirstOrDefaultAsync(expense => expense.Id == expenseId, cancellationToken);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var aggregates = ChangeTracker
            .Entries<IAggregateRoot>()
            .Where(e => e.Entity.DomainEvents.Any())
            .Select(e => e.Entity)
            .ToList();

        var outboxMessages = aggregates
            .SelectMany(entity => entity.DomainEvents)
            .Select(CreateOutboxMessage)
            .ToList();

        OutboxMessages.AddRange(outboxMessages);

        foreach (var aggregate in aggregates)
        {
            aggregate.ClearDomainEvents();
        }

        return await base.SaveChangesAsync(cancellationToken);
    }

    private static OutboxMessage CreateOutboxMessage(IDomainEvent domainEvent)
    {
        IntegrationEvent integrationEvent = domainEvent switch
        {
            ExpenseCreatedDomainEvent createdEvent => new ExpenseCreatedIntegrationEvent(
                createdEvent.ExpenseId,
                createdEvent.EmployeeId,
                createdEvent.DepartmentId,
                createdEvent.Amount,
                createdEvent.Currency,
                createdEvent.Description)
            {
                Id = createdEvent.EventId,
                OccurredOnUtc = createdEvent.OccurredOnUtc
            },
            ExpenseApprovedDomainEvent approvedEvent => new ExpenseApprovedIntegrationEvent(
                approvedEvent.ExpenseId,
                approvedEvent.ApproverId,
                approvedEvent.ApproverDepartmentId)
            {
                Id = approvedEvent.EventId,
                OccurredOnUtc = approvedEvent.OccurredOnUtc
            },
            ExpenseRejectedDomainEvent rejectedEvent => new ExpenseRejectedIntegrationEvent(rejectedEvent.ExpenseId)
            {
                Id = rejectedEvent.EventId,
                OccurredOnUtc = rejectedEvent.OccurredOnUtc
            },
            _ => throw new InvalidOperationException($"No integration event mapping exists for '{domainEvent.GetType().Name}'.")
        };

        var messageType = integrationEvent.GetType();

        return new OutboxMessage
        {
            Id = integrationEvent.Id,
            OccurredOnUtc = integrationEvent.OccurredOnUtc,
            Type = messageType.AssemblyQualifiedName ?? messageType.FullName ?? messageType.Name,
            Content = JsonSerializer.Serialize((object)integrationEvent, messageType)
        };
    }
}
