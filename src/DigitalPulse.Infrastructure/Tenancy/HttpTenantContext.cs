using DigitalPulse.Application.Abstractions;
using DigitalPulse.Application.Common;
using Microsoft.AspNetCore.Http;

namespace DigitalPulse.Infrastructure.Tenancy;

public sealed class HttpTenantContext : ITenantContext
{
    public const string ItemsKey = "DigitalPulse.TenantId";

    private readonly IHttpContextAccessor _http;

    public HttpTenantContext(IHttpContextAccessor http) => _http = http;

    public Guid? TenantId
    {
        get
        {
            if (AmbientTenant.Current is Guid ambient)
            {
                return ambient;
            }

            if (_http.HttpContext?.Items[ItemsKey] is Guid fromItems)
            {
                return fromItems;
            }

            var claim = _http.HttpContext?.User.FindFirst("tenant_id")?.Value;
            return Guid.TryParse(claim, out var id) ? id : null;
        }
    }

    public Guid RequireTenantId() =>
        TenantId ?? throw AppException.Validation("Create a tenant before continuing.");

    public static void Set(HttpContext context, Guid tenantId) =>
        context.Items[ItemsKey] = tenantId;
}
