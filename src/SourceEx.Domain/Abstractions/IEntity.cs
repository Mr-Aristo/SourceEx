namespace SourceEx.Domain.Abstractions;

public interface IEntity<TId>
{
    TId Id { get; }
    DateTime CreatedAt { get; }
}