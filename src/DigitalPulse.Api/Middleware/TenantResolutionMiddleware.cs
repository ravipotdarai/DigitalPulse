using DigitalPulse.Infrastructure.Tenancy;
using System.Security.Claims;

namespace DigitalPulse.Api.Middleware;

public sealed class TenantResolutionMiddleware
{
    private readonly RequestDelegate _next;

    public TenantResolutionMiddleware(RequestDelegate next) => _next = next;

    public async Task Invoke(HttpContext context)
    {
        if (context.Request.Headers.TryGetValue("X-Tenant-Id", out var supplied)
            && !string.IsNullOrWhiteSpace(supplied))
        {
            var claim = context.User.FindFirstValue("tenant_id");
            if (!Guid.TryParse(claim, out var trusted) ||
                !Guid.TryParse(supplied.ToString(), out var posted) ||
                posted != trusted)
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                await context.Response.WriteAsJsonAsync(new
                {
                    type = "https://httpstatuses.com/400",
                    title = "Invalid tenant header",
                    detail = "TenantId must be resolved from the authenticated session, not supplied by the client."
                });
                return;
            }
        }

        var tenantClaim = context.User.FindFirstValue("tenant_id");
        if (Guid.TryParse(tenantClaim, out var tenantId))
        {
            HttpTenantContext.Set(context, tenantId);
        }

        await _next(context);
    }
}
