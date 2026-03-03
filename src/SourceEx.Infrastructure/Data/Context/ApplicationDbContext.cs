using Microsoft.EntityFrameworkCore;
using SourceEx.Application.Data;
using SourceEx.Domain.Abstractions;
using SourceEx.Domain.Models;
using SourceEx.Infrastructure.Outbox;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SourceEx.Infrastructure.Data.Context;

public class ApplicationDbContext : DbContext, IApplicationDbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options ): base(options)
    {}

    public DbSet<Expense> Expenses => Set<Expense>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Yazdığımız Configurations (ExpenseConfiguration vb.) sınıflarını otomatik bulup uygular
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

        // 2. Eventleri OutboxMessage'a çevir
        var outboxMessages = aggregates
            .SelectMany(entity => entity.DomainEvents)
            .Select(domainEvent => new OutboxMessage
            {
                Id = Guid.NewGuid(),
                OccurredOn = DateTime.UtcNow,
                Type = domainEvent.GetType().Name,
                Content = JsonSerializer.Serialize(domainEvent, new JsonSerializerOptions { ReferenceHandler = ReferenceHandler.IgnoreCycles })
            })
            .ToList();

        // 3. Tabloya ekle ve Aggregate'lerin içini temizle
        OutboxMessages.AddRange(outboxMessages);

        foreach (var aggregate in aggregates)
        {
            aggregate.ClearDomainEvents();
        }

        return await base.SaveChangesAsync(cancellationToken);
    }
}
