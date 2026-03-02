using SourceEx.Domain.Models;

namespace SourceEx.Application.Data;

public interface IApplicationDbContext
{
    /// <summary>
    /// Gets the collection of expenses in the database context.
    /// </summary>
    DbSet<Expense> Expenses { get; }

    /// <summary>
    /// Asynchronously saves all changes made in this context to the underlying database.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the asynchronous save operation.</param>
    /// <returns>A task that represents the asynchronous save operation. The task result contains the number of state entries
    /// written to the database.</returns>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
