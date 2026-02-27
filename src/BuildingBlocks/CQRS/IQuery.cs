namespace BuildingBlocks.CQRS;

/// <summary>
/// Represents a query in the CQRS architecture. 
/// Queries are used to retrieve data from the database without modifying the system's state.
/// </summary>
/// <typeparam name="TResponse">The type of the response returned by the query. It cannot be null.</typeparam>
public interface IQuery<TResponse> : IRequest<TResponse> where TResponse :notnull
{
}
