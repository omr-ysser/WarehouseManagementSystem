using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Warehouse.Application.Exceptions;

namespace Warehouse.Api.Middleware;

public sealed class ApiExceptionHandler : IExceptionHandler
{
    private readonly ILogger<ApiExceptionHandler> _logger;

    public ApiExceptionHandler(
        ILogger<ApiExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var status = StatusCodes.Status500InternalServerError;
        var detail = "An unexpected error occurred.";

        switch (exception)
        {
            case AppException app:
                status = app.StatusCode;
                detail = app.Message;
                break;

            case DbUpdateConcurrencyException:
                status = StatusCodes.Status409Conflict;
                detail = "The data changed. Reload and try again.";
                break;

            case DbUpdateException update
                when update.InnerException is SqlException
                {
                    Number: 2601 or 2627
                }:
                status = StatusCodes.Status409Conflict;
                detail = "A unique value already exists.";
                break;

            case DbUpdateException update
                when update.InnerException is SqlException
                {
                    Number: 547
                }:
                status = StatusCodes.Status409Conflict;
                detail = "The operation conflicts with related data or constraints.";
                break;

            case SqlException { Number: 1205 }:
                status = StatusCodes.Status409Conflict;
                detail = "A concurrent operation conflicted. Try again.";
                break;
        }

        if (status == 500)
        {
            _logger.LogError(exception, "Unhandled API exception.");
        }

        httpContext.Response.StatusCode = status;

        await httpContext.Response.WriteAsJsonAsync(
            new ProblemDetails
            {
                Status = status,
                Title = status switch
                {
                    400 => "Invalid request",
                    401 => "Unauthorized",
                    404 => "Resource not found",
                    409 => "Conflict",
                    _ => "Server error"
                },
                Detail = detail,
                Instance = httpContext.Request.Path.Value
            },
            cancellationToken: cancellationToken);

        return true;
    }
}