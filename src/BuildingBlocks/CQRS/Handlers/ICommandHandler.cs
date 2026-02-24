
namespace BuildingBlocks.CQRS.Handlers;

/// <summary>
/// Defines a handler for a specific command that does not return a value.
/// Responsible for executing business logic and state changes, returning a <see cref="Unit"/>.
/// </summary>
/// <typeparam name="TCommand">The type of the command being handled. Must implement <see cref="ICommand"/>.</typeparam>
public interface ICommandHandler<in TCommand>: ICommandHandler<TCommand,Unit>
    where TCommand : ICommand
{ }

/// <summary>
/// Defines a handler for a specific command that returns a value.
/// Responsible for executing business logic and returning the result.
/// </summary>
/// <typeparam name="TCommand">The type of the command being handled. Must implement <see cref="ICommand{TResponse}"/>.</typeparam>
/// <typeparam name="TResponse">The type of the response returned from the handler. It cannot be null.</typeparam>
public interface ICommandHandler<in TCommand,TResponse> : IRequestHandler<TCommand , TResponse>
    where TCommand : ICommand<TResponse>
    where TResponse : notnull 
{
}
