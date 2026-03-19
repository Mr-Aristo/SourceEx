using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;

namespace SourceEx.API.RateLimiting;

/// <summary>
/// Configures API rate limiting policies.
/// </summary>
public static class ApiRateLimiter
{
    public const string ReadPolicy = "expenses-read";
    public const string WritePolicy = "expenses-write";

    public static void Configure(RateLimiterOptions options)
    {
        options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

        options.OnRejected = async (context, cancellationToken) =>
        {
            var problemDetails = new ProblemDetails
            {
                Status = StatusCodes.Status429TooManyRequests,
                Title = "Rate limit exceeded.",
                Detail = "Too many requests were sent in a short period. Please retry later."
            };

            await context.HttpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken: cancellationToken);
        };

        options.AddPolicy(ReadPolicy, httpContext =>
            RateLimitPartition.GetFixedWindowLimiter(
                partitionKey: GetPartitionKey(httpContext),
                factory: _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 120,
                    Window = TimeSpan.FromMinutes(1),
                    QueueLimit = 0,
                    AutoReplenishment = true
                }));

        options.AddPolicy(WritePolicy, httpContext =>
            RateLimitPartition.GetSlidingWindowLimiter(
                partitionKey: GetPartitionKey(httpContext),
                factory: _ => new SlidingWindowRateLimiterOptions
                {
                    PermitLimit = 30,
                    Window = TimeSpan.FromMinutes(1),
                    SegmentsPerWindow = 6,
                    QueueLimit = 0,
                    AutoReplenishment = true
                }));
    }

    private static string GetPartitionKey(HttpContext httpContext)
    {
        if (httpContext.User.Identity?.IsAuthenticated == true)
            return httpContext.User.Identity.Name ?? "authenticated";

        return httpContext.Connection.RemoteIpAddress?.ToString() ?? "anonymous";
    }
}
