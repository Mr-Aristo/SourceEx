using System.Security.Claims;
using Asp.Versioning;
using Asp.Versioning.Builder;
using MediatR;
using SourceEx.API.Contracts;
using SourceEx.API.RateLimiting;
using SourceEx.API.Security;
using SourceEx.Application.Expenses.Commands.ApproveExpense;
using SourceEx.Application.Expenses.Commands.CreateExpense;
using SourceEx.Application.Expenses.Queries.GetExpenseById;

namespace SourceEx.API.Endpoints;

/// <summary>
/// Maps expense endpoints for the API surface.
/// </summary>
public static class ExpenseEndpoints
{
    public static IEndpointRouteBuilder MapExpenseEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var versionSet = endpoints.NewApiVersionSet()
            .HasApiVersion(new ApiVersion(1, 0))
            .ReportApiVersions()
            .Build();

        var group = endpoints.MapGroup("/api/v{version:apiVersion}/expenses")
            .WithApiVersionSet(versionSet)
            .WithGroupName("v1")
            .WithTags("Expenses")
            .RequireAuthorization(AuthorizationPolicies.AuthenticatedUser);

        group.MapPost("/", CreateExpenseAsync)
            .MapToApiVersion(new ApiVersion(1, 0))
            .RequireRateLimiting(ApiRateLimiter.WritePolicy)
            .WithName("CreateExpense")
            .WithSummary("Creates a new expense.")
            .WithDescription("Creates a pending expense request and stores the resulting integration event through the outbox pattern.")
            .Produces<CreatedExpenseResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .WithOpenApi();

        group.MapGet("/{expenseId:guid}", GetExpenseByIdAsync)
            .MapToApiVersion(new ApiVersion(1, 0))
            .RequireRateLimiting(ApiRateLimiter.ReadPolicy)
            .WithName("GetExpenseById")
            .WithSummary("Gets a single expense by identifier.")
            .WithDescription("Returns the current state of a single expense request.")
            .Produces<ExpenseResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .WithOpenApi();

        group.MapPost("/{expenseId:guid}/approve", ApproveExpenseAsync)
            .MapToApiVersion(new ApiVersion(1, 0))
            .RequireAuthorization(AuthorizationPolicies.ExpenseApprover)
            .RequireRateLimiting(ApiRateLimiter.WritePolicy)
            .WithName("ApproveExpense")
            .WithSummary("Approves a pending expense.")
            .WithDescription("Approves a pending expense using the identity and department claims in the JWT access token, then queues the approval integration event through the outbox pattern.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .WithOpenApi();

        return endpoints;
    }

    private static async Task<IResult> CreateExpenseAsync(
        CreateExpenseRequest request,
        ClaimsPrincipal user,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var expenseId = await sender.Send(new CreateExpenseCommand(
            user.GetRequiredUserId(),
            user.GetRequiredDepartmentId(),
            request.Amount,
            request.Currency,
            request.Description), cancellationToken);

        return TypedResults.Created($"/api/v1.0/expenses/{expenseId}", new CreatedExpenseResponse(expenseId));
    }

    private static async Task<IResult> GetExpenseByIdAsync(
        Guid expenseId,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var expense = await sender.Send(new GetExpenseByIdQuery(expenseId), cancellationToken);

        return TypedResults.Ok(new ExpenseResponse(
            expense.Id,
            expense.EmployeeId,
            expense.DepartmentId,
            expense.Amount,
            expense.Currency,
            expense.Description,
            expense.Status,
            expense.CreatedAt));
    }

    private static async Task<IResult> ApproveExpenseAsync(
        Guid expenseId,
        ClaimsPrincipal user,
        ISender sender,
        CancellationToken cancellationToken)
    {
        await sender.Send(new ApproveExpenseCommand(
            expenseId,
            user.GetRequiredUserId(),
            user.GetRequiredDepartmentId()), cancellationToken);

        return TypedResults.NoContent();
    }
}
