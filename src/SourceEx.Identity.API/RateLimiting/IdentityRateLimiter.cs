using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;

namespace SourceEx.Identity.API.RateLimiting;

/// <summary>
/// Configures rate limits for identity-sensitive endpoints.
/// </summary>
public static class IdentityRateLimiter
{
    public const string LoginPolicy = "identity-login";
    public const string RegistrationPolicy = "identity-register";
    public const string RefreshPolicy = "identity-refresh";

    public static void Configure(RateLimiterOptions options)
    {
        options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

        options.OnRejected = async (context, cancellationToken) =>
        {
            var problemDetails = new ProblemDetails
            {
                Status = StatusCodes.Status429TooManyRequests,
                Title = "Rate limit exceeded.",
                Detail = "Too many identity requests were sent in a short period. Please retry later."
            };

            await context.HttpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken: cancellationToken);
        };

        options.AddPolicy(LoginPolicy, httpContext =>
            RateLimitPartition.GetFixedWindowLimiter(
                partitionKey: GetPartitionKey(httpContext),
                factory: _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 10,
                    Window = TimeSpan.FromMinutes(1),
                    QueueLimit = 0,
                    AutoReplenishment = true
                }));

        options.AddPolicy(RegistrationPolicy, httpContext =>
            RateLimitPartition.GetFixedWindowLimiter(
                partitionKey: GetPartitionKey(httpContext),
                factory: _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 5,
                    Window = TimeSpan.FromMinutes(5),
                    QueueLimit = 0,
                    AutoReplenishment = true
                }));

        options.AddPolicy(RefreshPolicy, httpContext =>
            RateLimitPartition.GetSlidingWindowLimiter(
                partitionKey: GetPartitionKey(httpContext),
                factory: _ => new SlidingWindowRateLimiterOptions
                {
                    PermitLimit = 20,
                    Window = TimeSpan.FromMinutes(5),
                    SegmentsPerWindow = 5,
                    QueueLimit = 0,
                    AutoReplenishment = true
                }));
    }

    private static string GetPartitionKey(HttpContext httpContext)
        => httpContext.Connection.RemoteIpAddress?.ToString() ?? "anonymous";
}
