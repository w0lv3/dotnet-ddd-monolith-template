using System.Diagnostics;
using Example.Application.Exceptions;
using Example.Domain.Exceptions;
using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Example.Api.Middleware;

public sealed class ApiExceptionHandler(
    IProblemDetailsService problemDetailsService,
    ILogger<ApiExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var problemDetails = CreateProblemDetails(httpContext, exception);

        if (problemDetails.Status == StatusCodes.Status500InternalServerError)
        {
            logger.LogError(exception, "An unhandled exception occurred while processing the request.");
        }

        httpContext.Response.StatusCode = problemDetails.Status!.Value;

        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = problemDetails,
            Exception = exception
        });
    }

    private static ProblemDetails CreateProblemDetails(HttpContext httpContext, Exception exception)
    {
        ProblemDetails problemDetails = exception switch
        {
            ValidationException validationException => CreateValidationProblemDetails(validationException),
            ArgumentException argumentException => CreateProblemDetails(
                StatusCodes.Status400BadRequest,
                "Bad request",
                argumentException.Message),
            ExampleNotFoundException notFoundException => CreateProblemDetails(
                StatusCodes.Status404NotFound,
                "Resource not found",
                notFoundException.Message),
            DomainException domainException => CreateProblemDetails(
                StatusCodes.Status409Conflict,
                "Domain conflict",
                domainException.Message),
            UnauthorizedAccessException unauthorizedException => CreateProblemDetails(
                httpContext.User.Identity?.IsAuthenticated == true
                    ? StatusCodes.Status403Forbidden
                    : StatusCodes.Status401Unauthorized,
                httpContext.User.Identity?.IsAuthenticated == true ? "Forbidden" : "Unauthorized",
                unauthorizedException.Message),
            _ => CreateProblemDetails(
                StatusCodes.Status500InternalServerError,
                "Internal server error",
                "An unexpected error occurred.")
        };

        problemDetails.Instance = httpContext.Request.Path;
        problemDetails.Extensions["traceId"] = Activity.Current?.Id ?? httpContext.TraceIdentifier;

        return problemDetails;
    }

    private static HttpValidationProblemDetails CreateValidationProblemDetails(
        ValidationException exception)
    {
        var errors = exception.Errors
            .GroupBy(failure => failure.PropertyName)
            .ToDictionary(
                group => group.Key,
                group => group.Select(failure => failure.ErrorMessage).Distinct().ToArray());

        return new HttpValidationProblemDetails(errors)
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "Validation failed",
            Detail = "One or more validation errors occurred.",
            Type = GetProblemType(StatusCodes.Status400BadRequest)
        };
    }

    private static ProblemDetails CreateProblemDetails(int status, string title, string detail)
    {
        return new ProblemDetails
        {
            Status = status,
            Title = title,
            Detail = detail,
            Type = GetProblemType(status)
        };
    }

    private static string GetProblemType(int status)
    {
        return status switch
        {
            StatusCodes.Status400BadRequest => "https://www.rfc-editor.org/rfc/rfc9110#name-400-bad-request",
            StatusCodes.Status401Unauthorized => "https://www.rfc-editor.org/rfc/rfc9110#name-401-unauthorized",
            StatusCodes.Status403Forbidden => "https://www.rfc-editor.org/rfc/rfc9110#name-403-forbidden",
            StatusCodes.Status404NotFound => "https://www.rfc-editor.org/rfc/rfc9110#name-404-not-found",
            StatusCodes.Status409Conflict => "https://www.rfc-editor.org/rfc/rfc9110#name-409-conflict",
            _ => "https://www.rfc-editor.org/rfc/rfc9110#name-500-internal-server-error"
        };
    }
}
