using EnterpriseFlow.Application.Common;
using EnterpriseFlow.Domain.Exceptions;
using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace EnterpriseFlow.Api.Middleware;

/// <summary>
/// Single translation point from exceptions to HTTP ProblemDetails responses, so every
/// endpoint gets consistent error shapes without repeating try/catch per handler.
/// </summary>
public sealed partial class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var (statusCode, title, extensions) = Map(exception);

        LogUnhandledException(logger, statusCode, exception);

        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Type = $"https://httpstatuses.com/{statusCode}",
        };

        foreach (var (key, value) in extensions)
        {
            problemDetails.Extensions[key] = value;
        }

        httpContext.Response.StatusCode = statusCode;
        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

        return true;
    }

    private static (int StatusCode, string Title, Dictionary<string, object?> Extensions) Map(Exception exception) =>
        exception switch
        {
            ValidationException validationException => (
                StatusCodes.Status400BadRequest,
                "One or more validation errors occurred.",
                new Dictionary<string, object?>
                {
                    ["errors"] = validationException.Errors
                        .GroupBy(e => e.PropertyName)
                        .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray()),
                }),
            ForbiddenAccessException => (StatusCodes.Status403Forbidden, "Forbidden.", []),
            InvalidCredentialsException or InvalidRefreshTokenException =>
                (StatusCodes.Status401Unauthorized, exception.Message, []),
            NotFoundException => (StatusCodes.Status404NotFound, exception.Message, []),
            RegistrationFailedException => (StatusCodes.Status400BadRequest, exception.Message, []),
            DomainException => (StatusCodes.Status400BadRequest, exception.Message, []),
            _ => (StatusCodes.Status500InternalServerError, "An unexpected error occurred.", []),
        };

    [LoggerMessage(Level = LogLevel.Error, Message = "Unhandled exception mapped to HTTP {StatusCode}")]
    private static partial void LogUnhandledException(ILogger logger, int statusCode, Exception exception);
}
