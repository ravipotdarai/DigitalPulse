using DigitalPulse.Application.Abstractions;
using DigitalPulse.Application.Common;
using DigitalPulse.Application.Features.Identity;
using DigitalPulse.Contracts.Directories;
using DigitalPulse.Domain.Businesses;
using DigitalPulse.Domain.Directories;
using DigitalPulse.Domain.Platforms;
using Microsoft.EntityFrameworkCore;

namespace DigitalPulse.Application.Features.Directories;

internal static class DirectoryCatalog
{
    public static readonly HashSet<string> Allowed = new(StringComparer.OrdinalIgnoreCase) { "INDIAMART", "JUSTDIAL" };

    public static DirectoryCapabilityResponse Capabilities(IPlatformAdapter adapter)
    {
        var d = adapter.Describe();
        return new(
            d.Code,
            d.Name,
            d.Capabilities.CanRead,
            false,
            d.Capabilities.AssistedOnly,
            "Hold — no official authorized read API is configured. Listings are not scraped.",
            "Assisted — DigitalPulse will not invent an unofficial write API.");
    }

    public static IReadOnlyList<(string Title, string Detail)> Playbook(string platformName, string name, string? phone, string? website)
    {
        var label = string.IsNullOrWhiteSpace(phone) ? "the phone on the identity record" : phone;
        var site = string.IsNullOrWhiteSpace(website) ? "the official website on the identity record" : website;
        return
        [
            ($"Sign in to {platformName}", $"Use the official {platformName} seller or business login. DigitalPulse does not store that password."),
            ("Open the company profile", "Stay on the official profile editor. Do not use a third-party listing tool."),
            ("Set the company name", $"Copy the canonical name: {name}"),
            ("Set the phone", $"Copy {label}. Restricted facts stay off the listing."),
            ("Set the website", $"Copy {site}."),
            ("Confirm the public listing", $"Save on {platformName}, then return here to verify. DigitalPulse cannot confirm the live page without an official read API.")
        ];
    }

    public static DirectoryStepResponse ToResponse(this DirectoryStep step) =>
        new(step.Id, step.Ordinal, step.Title, step.Detail, step.CompletedAtUtc);

    public static DirectoryTaskResponse ToResponse(this DirectoryTask task) =>
        new(
            task.Id,
            task.PlatformCode,
            task.Kind.ToString(),
            task.Status.ToString(),
            task.PreparedName,
            task.PreparedPhone,
            task.PreparedWebsite,
            task.PreparedCategory,
            task.PreparedServices,
            task.VerificationNote,
            task.VerifiedAtUtc,
            task.LastMonitoredAtUtc,
            task.MonitorDetail,
            task.Steps.OrderBy(s => s.Ordinal).Select(s => s.ToResponse()).ToList());
}

public sealed class GetDirectoryWorkspaceHandler
{
    private readonly IAppDbContext _db;
    private readonly ITenantContext _tenant;
    private readonly IPlatformAdapterCatalog _catalog;

    public GetDirectoryWorkspaceHandler(IAppDbContext db, ITenantContext tenant, IPlatformAdapterCatalog catalog)
    {
        _db = db;
        _tenant = tenant;
        _catalog = catalog;
    }

    public async Task<DirectoryWorkspaceResponse> Handle(Guid businessId, CancellationToken cancellationToken)
    {
        var tenantId = _tenant.RequireTenantId();
        await BusinessAccess.RequireAsync(_db, tenantId, businessId, cancellationToken);
        var connections = await _db.Connections.AsNoTracking()
            .Where(c => c.BusinessId == businessId && DirectoryCatalog.Allowed.Contains(c.PlatformCode))
            .ToListAsync(cancellationToken);
        var tasks = await LoadTasksAsync(_db, businessId, cancellationToken);

        var providers = DirectoryCatalog.Allowed
            .Select(code =>
            {
                var adapter = _catalog.Get(code);
                var link = connections.FirstOrDefault(c => c.PlatformCode.Equals(code, StringComparison.OrdinalIgnoreCase));
                return new DirectoryProviderResponse(
                    DirectoryCatalog.Capabilities(adapter),
                    link?.Status.ToString(),
                    link?.GrantKind,
                    link?.LastHealthStatus,
                    "Hold",
                    "Authorized official read is unavailable. DigitalPulse will not scrape IndiaMART or Justdial.");
            })
            .ToList();

        return new DirectoryWorkspaceResponse(
            providers,
            tasks.Select(t => t.ToResponse()).ToList(),
            "IndiaMART and Justdial use assisted playbooks from the canonical identity. Official writes and scrapes are not invented.");
    }

    internal static async Task<List<DirectoryTask>> LoadTasksAsync(IAppDbContext db, Guid businessId, CancellationToken cancellationToken)
    {
        var tasks = await db.DirectoryTasks.AsNoTracking()
            .Where(t => t.BusinessId == businessId)
            .OrderByDescending(t => t.UpdatedAtUtc)
            .ToListAsync(cancellationToken);
        var ids = tasks.Select(t => t.Id).ToList();
        var steps = await db.DirectorySteps.AsNoTracking()
            .Where(s => ids.Contains(s.TaskId))
            .ToListAsync(cancellationToken);
        foreach (var task in tasks)
        {
            task.AttachSteps(steps.Where(s => s.TaskId == task.Id));
        }

        return tasks;
    }
}

public sealed class PrepareDirectoryTaskHandler
{
    private readonly IAppDbContext _db;
    private readonly ITenantContext _tenant;
    private readonly IPlatformAdapterCatalog _catalog;

    public PrepareDirectoryTaskHandler(IAppDbContext db, ITenantContext tenant, IPlatformAdapterCatalog catalog)
    {
        _db = db;
        _tenant = tenant;
        _catalog = catalog;
    }

    public async Task<DirectoryTaskResponse> Handle(Guid businessId, PrepareDirectoryRequest request, CancellationToken cancellationToken)
    {
        var tenantId = _tenant.RequireTenantId();
        var business = await BusinessAccess.RequireAsync(_db, tenantId, businessId, cancellationToken);
        if (string.IsNullOrWhiteSpace(request.PlatformCode) || !DirectoryCatalog.Allowed.Contains(request.PlatformCode))
        {
            throw AppException.Validation("Choose IndiaMART or Justdial. Social and WhatsApp are not directories.");
        }

        var adapter = _catalog.Get(request.PlatformCode);
        if (!adapter.Describe().Capabilities.AssistedOnly)
        {
            throw AppException.Validation("This provider is not an assisted directory in the catalog.");
        }

        var connection = await _db.Connections.AsNoTracking()
            .FirstOrDefaultAsync(c => c.BusinessId == businessId && c.PlatformCode == request.PlatformCode.Trim().ToUpperInvariant(), cancellationToken);
        if (connection is null || connection.Status != ConnectionStatus.Connected)
        {
            throw AppException.Validation("Enable the assisted connection in Connection Center before preparing a directory playbook.");
        }

        var phone = await _db.ContactPoints.AsNoTracking()
            .Where(c => c.BusinessId == businessId && c.Kind == ContactPointKind.Phone)
            .Select(c => c.Value)
            .FirstOrDefaultAsync(cancellationToken);
        var category = await _db.Categories.AsNoTracking()
            .Where(c => c.BusinessId == businessId)
            .Select(c => c.Name)
            .FirstOrDefaultAsync(cancellationToken);
        var services = await _db.Services.AsNoTracking()
            .Where(s => s.BusinessId == businessId)
            .Select(s => s.Name)
            .ToListAsync(cancellationToken);

        var task = DirectoryTask.Prepare(
            tenantId,
            businessId,
            request.PlatformCode,
            business.Name,
            phone,
            business.Website,
            category,
            services.Count == 0 ? null : string.Join(", ", services),
            DirectoryCatalog.Playbook(adapter.Describe().Name, business.Name, phone, business.Website));
        _db.DirectoryTasks.Add(task);
        foreach (var step in task.Steps)
        {
            _db.DirectorySteps.Add(step);
        }

        await _db.SaveChangesAsync(cancellationToken);
        return task.ToResponse();
    }
}

public sealed class CompleteDirectoryStepHandler
{
    private readonly IAppDbContext _db;
    private readonly ITenantContext _tenant;

    public CompleteDirectoryStepHandler(IAppDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    public async Task<DirectoryTaskResponse> Handle(Guid businessId, Guid taskId, Guid stepId, CancellationToken cancellationToken)
    {
        var tenantId = _tenant.RequireTenantId();
        await BusinessAccess.RequireAsync(_db, tenantId, businessId, cancellationToken);
        var task = await _db.DirectoryTasks.FirstOrDefaultAsync(t => t.Id == taskId && t.BusinessId == businessId, cancellationToken)
            ?? throw AppException.NotFound("Directory task was not found.");
        var steps = await _db.DirectorySteps.Where(s => s.TaskId == task.Id).ToListAsync(cancellationToken);
        task.AttachSteps(steps);
        try
        {
            task.CompleteStep(stepId);
        }
        catch (InvalidOperationException ex)
        {
            throw AppException.Validation(ex.Message);
        }

        await _db.SaveChangesAsync(cancellationToken);
        return task.ToResponse();
    }
}

public sealed class VerifyDirectoryTaskHandler
{
    private readonly IAppDbContext _db;
    private readonly ITenantContext _tenant;

    public VerifyDirectoryTaskHandler(IAppDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    public async Task<DirectoryTaskResponse> Handle(Guid businessId, Guid taskId, VerifyDirectoryRequest request, CancellationToken cancellationToken)
    {
        var tenantId = _tenant.RequireTenantId();
        await BusinessAccess.RequireAsync(_db, tenantId, businessId, cancellationToken);
        var task = await _db.DirectoryTasks.FirstOrDefaultAsync(t => t.Id == taskId && t.BusinessId == businessId, cancellationToken)
            ?? throw AppException.NotFound("Directory task was not found.");
        var steps = await _db.DirectorySteps.Where(s => s.TaskId == task.Id).ToListAsync(cancellationToken);
        task.AttachSteps(steps);
        try
        {
            task.Verify(request.Note);
        }
        catch (InvalidOperationException ex)
        {
            throw AppException.Validation(ex.Message);
        }

        await _db.SaveChangesAsync(cancellationToken);
        return task.ToResponse();
    }
}

public sealed class MonitorDirectoryHandler
{
    private readonly IAppDbContext _db;
    private readonly ITenantContext _tenant;
    private readonly IPlatformAdapterCatalog _catalog;

    public MonitorDirectoryHandler(IAppDbContext db, ITenantContext tenant, IPlatformAdapterCatalog catalog)
    {
        _db = db;
        _tenant = tenant;
        _catalog = catalog;
    }

    public async Task<DirectoryWorkspaceResponse> Handle(Guid businessId, string platformCode, CancellationToken cancellationToken)
    {
        var tenantId = _tenant.RequireTenantId();
        await BusinessAccess.RequireAsync(_db, tenantId, businessId, cancellationToken);
        if (!DirectoryCatalog.Allowed.Contains(platformCode))
        {
            throw AppException.Validation("Choose IndiaMART or Justdial.");
        }

        var adapter = _catalog.Get(platformCode);
        var connection = await _db.Connections.FirstOrDefaultAsync(
            c => c.BusinessId == businessId && c.PlatformCode == platformCode.Trim().ToUpperInvariant(), cancellationToken);
        var latest = await _db.DirectoryTasks
            .Where(t => t.BusinessId == businessId && t.PlatformCode == platformCode.Trim().ToUpperInvariant())
            .OrderByDescending(t => t.UpdatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

        var health = connection is null
            ? "Assisted connection is not enabled."
            : (await adapter.HealthCheckAsync(connection, cancellationToken)).Detail;
        var verify = latest?.Status == DirectoryTaskStatus.Verified
            ? $"Last verified {latest.VerifiedAtUtc:u}."
            : "No verified assisted update yet.";
        string detail;
        if (connection is { HasLiveCredential: true })
        {
            var metrics = await adapter.MetricsAsync(connection, cancellationToken);
            detail = $"{health} {metrics.Detail} Profile writes stay assisted. {verify}";
        }
        else
        {
            detail = $"{health} Official listing reads remain on hold. {verify}";
        }

        if (latest is not null)
        {
            latest.RecordMonitor(detail);
        }

        await _db.SaveChangesAsync(cancellationToken);
        return await new GetDirectoryWorkspaceHandler(_db, _tenant, _catalog).Handle(businessId, cancellationToken);
    }
}
