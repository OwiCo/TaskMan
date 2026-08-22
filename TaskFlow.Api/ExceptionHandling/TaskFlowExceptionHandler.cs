using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using TaskFlow.Api.Domain.Exceptions;

namespace TaskFlow.Api.ExceptionHandling;

/// <summary>
/// Single place that translates exceptions into RFC 9457 ProblemDetails responses, so controllers
/// never need try/catch. Anything not explicitly matched below becomes a generic 500 - detail goes
/// to the log, never to the client.
/// </summary>
public sealed class TaskFlowExceptionHandler(ILogger<TaskFlowExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var problem = Map(exception, httpContext.TraceIdentifier);

        if (problem.Status == StatusCodes.Status500InternalServerError)
        {
            logger.LogError(exception, "Unhandled exception.");
        }

        httpContext.Response.StatusCode = problem.Status!.Value;
        await httpContext.Response.WriteAsJsonAsync(problem, cancellationToken);
        return true;
    }

    private static ProblemDetails Map(Exception exception, string traceId)
    {
        ProblemDetails problem = exception switch
        {
            NotFoundException ex => new ProblemDetails
            {
                Title = "Not found.",
                Status = StatusCodes.Status404NotFound,
                Detail = ex.Message,
            },

            ArgumentException ex => new ProblemDetails
            {
                Title = "Invalid request.",
                Status = StatusCodes.Status400BadRequest,
                Detail = ex.Message,
            },

            InvalidTransitionException ex => new ProblemDetails
            {
                Title = "Illegal status transition.",
                Status = StatusCodes.Status409Conflict,
                Detail = ex.Message,
                Extensions = { ["from"] = ex.From.ToString(), ["to"] = ex.To.ToString() },
            },

            InvalidHierarchyException ex => new ProblemDetails
            {
                Title = "Illegal work item hierarchy.",
                Status = StatusCodes.Status409Conflict,
                Detail = ex.Message,
                Extensions = { ["childType"] = ex.ChildType.ToString(), ["parentType"] = ex.ParentType?.ToString() },
            },

            DuplicateKeyException ex => new ProblemDetails
            {
                Title = "Duplicate value.",
                Status = StatusCodes.Status409Conflict,
                Detail = ex.Message,
            },

            // Belt-and-suspenders: the application-level pre-check is what usually catches this, but
            // the database's unique constraint is the actual guarantee under a race - see DECISIONS.md.
            DbUpdateException { InnerException: PostgresException { SqlState: "23505" } } => new ProblemDetails
            {
                Title = "Duplicate value.",
                Status = StatusCodes.Status409Conflict,
                Detail = "A record with the same unique value already exists.",
            },

            DbUpdateException { InnerException: PostgresException { SqlState: "23503" } } => new ProblemDetails
            {
                Title = "Referenced by other records.",
                Status = StatusCodes.Status409Conflict,
                Detail = "This record cannot be deleted or changed because other records still reference it.",
            },

            _ => new ProblemDetails
            {
                Title = "An unexpected error occurred.",
                Status = StatusCodes.Status500InternalServerError,
            },
        };

        problem.Extensions["traceId"] = traceId;
        return problem;
    }
}
