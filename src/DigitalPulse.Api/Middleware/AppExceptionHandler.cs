using DigitalPulse.Application.Common;
using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;

namespace DigitalPulse.Api.Middleware;

public sealed class AppExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken cancellationToken)
    {
        var (status, code, title) = exception switch
        {
            AppException app => (app.StatusCode, app.Code, app.Message),
            ValidationException validation => (400, "validation_failed", string.Join(" ", validation.Errors.Select(e => e.ErrorMessage))),
            InvalidOperationException invalid => (400, "validation_failed", invalid.Message),
            _ => (500, "server_error", "An unexpected error occurred.")
        };

        context.Response.StatusCode = status;
        await context.Response.WriteAsJsonAsync(new
        {
            type = $"https://httpstatuses.com/{status}",
            title,
            status,
            code
        }, cancellationToken);
        return true;
    }
}
