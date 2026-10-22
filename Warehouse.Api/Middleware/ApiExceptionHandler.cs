using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Warehouse.Application.Exceptions;

namespace Warehouse.Api.Middleware;

public sealed class ApiExceptionHandler : IExceptionHandler
{
    private readonly ILogger<ApiExceptionHandler> _logger;

    public ApiExceptionHandler(ILogger<ApiExceptionHandler> logger)
    {
        _logger = logger;
    }

    private static SqlException? FindSqlException(Exception exception)
    {
        Exception? current = exception;

        while (current is not null)
        {
            if (current is SqlException sql)
            {
                return sql;
            }

            current = current.InnerException;
        }

        return null;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        ProblemDetails problem;

        if (exception is ValidationException validation)
        {
            var errors = validation.Errors
                .GroupBy(x => x.PropertyName)
                .ToDictionary(
                    x => x.Key,
                    x => x.Select(e => e.ErrorMessage)
                        .Distinct()
                        .ToArray());

            problem = new ValidationProblemDetails(errors)
            {
                Status = 400,
                Title = "Validation failed"
            };
        }
        else
        {
            var sql = FindSqlException(exception);

            var status = 500;
            var detail = "An unexpected error occurred.";

            if (exception is AppException app)
            {
                status = app.StatusCode;
                detail = app.Message;
            }
            else if (exception is DbUpdateConcurrencyException)
            {
                status = 409;
                detail = "The data changed. Reload and try again.";
            }
            else if (sql?.Number is 2601 or 2627)
            {
                status = 409;
                detail =
                    "A unique value already exists or a concurrent insert conflicted.";
            }
            else if (sql?.Number == 547)
            {
                status = 409;
                detail =
                    "The operation conflicts with related data or database constraints.";
            }
            else if (sql?.Number == 1205)
            {
                status = 409;
                detail =
                    "A concurrent operation conflicted. Reload and try again.";
            }

            if (status == 500)
            {
                _logger.LogError(
                    exception,
                    "Unhandled API exception. TraceId: {TraceId}",
                    httpContext.TraceIdentifier);
            }

            problem = new ProblemDetails
            {
                Status = status,
                Title = status switch
                {
                    400 => "Invalid request",
                    401 => "Unauthorized",
                    403 => "Forbidden",
                    404 => "Resource not found",
                    409 => "Conflict",
                    _ => "Server error"
                },
                Detail = detail
            };
        }

        problem.Instance = httpContext.Request.Path.Value;
        problem.Extensions["traceId"] = httpContext.TraceIdentifier;

        httpContext.Response.StatusCode = problem.Status ?? 500;

        await httpContext.Response.WriteAsJsonAsync(
            problem,
            options: null,
            contentType: "application/problem+json",
            cancellationToken: cancellationToken);

        return true;
    }
}