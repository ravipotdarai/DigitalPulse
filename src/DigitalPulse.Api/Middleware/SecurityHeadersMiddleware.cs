namespace DigitalPulse.Api.Middleware;

public sealed class CorrelationMiddleware
{
    public const string HeaderName = "X-Correlation-Id";
    private readonly RequestDelegate _next;

    public CorrelationMiddleware(RequestDelegate next) => _next = next;

    public async Task Invoke(HttpContext context)
    {
        var incoming = context.Request.Headers[HeaderName].ToString();
        var id = Guid.TryParse(incoming, out var parsed) ? parsed.ToString("D") : Guid.NewGuid().ToString("D");
        context.Items[HeaderName] = id;
        context.Response.OnStarting(() =>
        {
            context.Response.Headers[HeaderName] = id;
            return Task.CompletedTask;
        });
        await _next(context);
    }
}

public sealed class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;

    public SecurityHeadersMiddleware(RequestDelegate next) => _next = next;

    public Task Invoke(HttpContext context)
    {
        context.Response.OnStarting(() =>
        {
            var headers = context.Response.Headers;
            headers["X-Content-Type-Options"] = "nosniff";
            headers["X-Frame-Options"] = "DENY";
            headers["Referrer-Policy"] = "no-referrer";
            headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";
            headers["Content-Security-Policy"] = "default-src 'none'; frame-ancestors 'none'; base-uri 'none'";
            headers.Remove("Server");
            return Task.CompletedTask;
        });
        return _next(context);
    }
}

public sealed class ResponseTimingMiddleware
{
    private readonly RequestDelegate _next;

    public ResponseTimingMiddleware(RequestDelegate next) => _next = next;

    public async Task Invoke(HttpContext context)
    {
        var started = DateTime.UtcNow;
        context.Response.OnStarting(() =>
        {
            var ms = (int)Math.Max(0, (DateTime.UtcNow - started).TotalMilliseconds);
            context.Response.Headers["X-Response-Time-Ms"] = ms.ToString();
            return Task.CompletedTask;
        });
        await _next(context);
    }
}
