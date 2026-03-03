using SourceEx.Domain.Models;

namespace SourceEx.Application.Data;

/// <summary>
/// Defines the contract for an application database context that provides access to expense entities and supports
/// saving changes asynchronously.
/// </summary>
/// <remarks>Implementations of this interface are typically used to interact with the application's data store,
/// enabling querying and persisting of expense data. This interface is intended to be used with dependency injection to
/// facilitate testing and separation of concerns.</remarks>
public interface IApplicationDbContext
{
    DbSet<Expense> Expenses { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
