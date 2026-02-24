namespace BuildingBlocks.CQRS.Handlers;

/// <summary>
/// Defines a handler for a specific query type in the CQRS architecture.
/// Responsible for executing the query and returning the requested data.
/// </summary>
/// <typeparam name="TQuery">The type of the query being handled. Must implement <see cref="IQuery{TResponse}"/>.</typeparam>
/// <typeparam name="TResponse">The type of the response returned from the handler. It cannot be null.</typeparam>
public interface IQueryHandler<in TQuery, TResponse>:IRequestHandler<TQuery,TResponse>
    where TQuery : IQuery<TResponse>
    where TResponse : notnull
{
}
