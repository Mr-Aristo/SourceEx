using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using SourceEx.Application.Exceptions;
using SourceEx.Domain.Exceptions;

namespace SourceEx.API.ExceptionHandling;

/// <summary>
/// Converts unhandled exceptions into consistent problem details responses.
/// </summary>
public sealed class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;
    private readonly IProblemDetailsService _problemDetailsService;

    public GlobalExceptionHandler(
        ILogger<GlobalExceptionHandler> logger,
        IProblemDetailsService problemDetailsService)
    {
        _logger = logger;
        _problemDetailsService = problemDetailsService;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var statusCode = exception switch
        {
            UnauthorizedAccessException => StatusCodes.Status401Unauthorized,
            SecurityTokenException => StatusCodes.Status401Unauthorized,
            RequestValidationException => StatusCodes.Status400BadRequest,
            DomainException => StatusCodes.Status400BadRequest,
            ArgumentException => StatusCodes.Status400BadRequest,
            KeyNotFoundException => StatusCodes.Status404NotFound,
            _ => StatusCodes.Status500InternalServerError
        };

        _logger.LogError(exception, "Unhandled exception encountered for request {RequestPath}.", httpContext.Request.Path);

        ProblemDetails problemDetails = exception switch
        {
            RequestValidationException validationException => new HttpValidationProblemDetails(
                validationException.Errors.ToDictionary(
                    pair => pair.Key,
                    pair => pair.Value))
            {
                Status = statusCode,
                Title = "One or more validation errors occurred.",
                Detail = "The request did not satisfy the application validation rules."
            },
            _ => new ProblemDetails
            {
                Status = statusCode,
                Title = statusCode switch
                {
                    StatusCodes.Status401Unauthorized => "Authentication is required.",
                    StatusCodes.Status400BadRequest => "The request could not be processed.",
                    StatusCodes.Status404NotFound => "The requested resource was not found.",
                    _ => "An unexpected error occurred."
                },
                Detail = statusCode >= 500 ? "The server encountered an unexpected error." : exception.Message
            },
        };

        problemDetails.Extensions["traceId"] = httpContext.TraceIdentifier;

        return await _problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = problemDetails
        });
    }
}
