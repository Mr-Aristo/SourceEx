namespace BuildingBlocks.CQRS;

/// <summary>
/// Represents a command in the CQRS architecture that modifies the system's state 
/// and does not return a specific response (returns a <see cref="Unit"/>).
/// </summary>
public interface ICommand : ICommand<Unit>
{
}

/// <summary>
/// Represents a command in the CQRS architecture that modifies the system's state 
/// and returns a specific response.
/// </summary>
/// <typeparam name="TResponse">The type of the response returned by the command.</typeparam>
public interface ICommand<out TResponse> : IRequest<TResponse>
{
}
