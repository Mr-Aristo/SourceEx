using FluentValidation;
using FluentValidation.Results;
using MediatR;
using SourceEx.Application.Exceptions;

namespace SourceEx.Application.Behaviors;

/// <summary>
/// Executes FluentValidation validators before the request reaches its handler.
/// </summary>
public sealed class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
    {
        _validators = validators;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (!_validators.Any())
            return await next();

        var context = new ValidationContext<TRequest>(request);

        var validationResults = await Task.WhenAll(
            _validators.Select(validator => validator.ValidateAsync(context, cancellationToken)));

        var failures = validationResults
            .SelectMany(result => result.Errors)
            .Where(failure => failure is not null)
            .GroupBy(failure => new { failure.PropertyName, failure.ErrorMessage })
            .Select(group => new ValidationFailure(group.Key.PropertyName, group.Key.ErrorMessage))
            .ToList();

        if (failures.Count != 0)
        {
            var errors = failures
                .GroupBy(failure => failure.PropertyName)
                .ToDictionary(
                    group => group.Key,
                    group => group.Select(failure => failure.ErrorMessage).Distinct().ToArray());

            throw new RequestValidationException(errors);
        }

        return await next();
    }
}
