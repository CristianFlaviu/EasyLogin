using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace EasyLoginAPI.Exceptions;

public class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var (statusCode, body) = exception switch
        {
            ValidationException ve => (400, (object)new
            {
                errors = ve.Errors
                    .GroupBy(e => e.PropertyName)
                    .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray())
            }),
            UnauthorizedAccessException => (401, new { message = "Unauthorised" }),
            KeyNotFoundException => (404, new { message = "Not found" }),
            InvalidOperationException ioe => (409, new { message = ioe.Message }),
            _ => (500, (object)new { message = "An unexpected error occurred" })
        };

        if (statusCode == 500)
            logger.LogError(exception, "Unhandled exception");

        httpContext.Response.StatusCode = statusCode;
        await httpContext.Response.WriteAsJsonAsync(body, cancellationToken);

        return true;
    }
}
