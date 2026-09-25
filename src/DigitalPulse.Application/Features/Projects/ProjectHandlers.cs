using DigitalPulse.Application.Abstractions;
using DigitalPulse.Application.Common;
using DigitalPulse.Application.Features.Identity;
using DigitalPulse.Contracts.Projects;
using DigitalPulse.Domain.Projects;
using Microsoft.EntityFrameworkCore;

namespace DigitalPulse.Application.Features.Projects;

internal static class ProjectMaps
{
    public static ProjectPermissionScope ParsePermission(string value) =>
        Enum.TryParse<ProjectPermissionScope>(value, true, out var parsed)
            ? parsed
            : throw AppException.Validation("Permission must be None, Partial, or Full.");

    public static ProjectConfidentiality ParseConfidentiality(string value) =>
        Enum.TryParse<ProjectConfidentiality>(value, true, out var parsed)
            ? parsed
            : throw AppException.Validation("Confidentiality must be Internal, Restricted, or Public.");

    public static ProjectPublicationStatus ParsePublication(string value) =>
        Enum.TryParse<ProjectPublicationStatus>(value, true, out var parsed)
            ? parsed
            : throw AppException.Validation("Publication status must be Draft, Ready, or Archived.");

    public static MediaKind ParseMedia(string value) =>
        Enum.TryParse<MediaKind>(value, true, out var parsed)
            ? parsed
            : throw AppException.Validation("Media kind must be Image, Video, or Document.");

    public static ProjectSummaryResponse ToSummary(this Project project) =>
        new(
            project.Id,
            project.BusinessId,
            project.Name,
            project.ClientName,
            project.PermissionScope.ToString(),
            project.Confidentiality.ToString(),
            project.PublicationStatus.ToString(),
            project.UpdatedAtUtc);
}

public sealed class GetProjectWorkspaceHandler
{
    private readonly IAppDbContext _db;
    private readonly ITenantContext _tenant;

    public GetProjectWorkspaceHandler(IAppDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    public async Task<ProjectWorkspaceResponse> Handle(Guid businessId, CancellationToken cancellationToken)
    {
        var tenantId = _tenant.RequireTenantId();
        await BusinessAccess.RequireAsync(_db, tenantId, businessId, cancellationToken);
        var projects = await _db.Projects.AsNoTracking()
            .Where(p => p.BusinessId == businessId)
            .OrderByDescending(p => p.UpdatedAtUtc)
            .ToListAsync(cancellationToken);
        var services = await _db.Services.AsNoTracking()
            .Where(s => s.BusinessId == businessId)
            .OrderBy(s => s.Name)
            .Select(s => new NamedOptionResponse(s.Id, s.Name))
            .ToListAsync(cancellationToken);
        var brands = await _db.BusinessBrands.AsNoTracking()
            .Where(b => b.BusinessId == businessId)
            .Join(_db.Brands.AsNoTracking(), link => link.BrandId, brand => brand.Id, (_, brand) => new NamedOptionResponse(brand.Id, brand.Name))
            .ToListAsync(cancellationToken);

        return new ProjectWorkspaceResponse(
            projects.Select(p => p.ToSummary()).ToList(),
            services,
            brands,
            "Projects stay on the identity record. Content factory drafts are assembled from stored fields. Live publishes and AI rewrites are not invented.");
    }
}

public sealed class GetProjectHandler
{
    private readonly IAppDbContext _db;
    private readonly ITenantContext _tenant;

    public GetProjectHandler(IAppDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    public async Task<ProjectDetailResponse> Handle(Guid businessId, Guid projectId, CancellationToken cancellationToken)
    {
        var tenantId = _tenant.RequireTenantId();
        await BusinessAccess.RequireAsync(_db, tenantId, businessId, cancellationToken);
        return await ProjectComposer.LoadAsync(_db, businessId, projectId, cancellationToken);
    }
}

public sealed class CreateProjectHandler
{
    private readonly IAppDbContext _db;
    private readonly ITenantContext _tenant;

    public CreateProjectHandler(IAppDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    public async Task<ProjectDetailResponse> Handle(Guid businessId, CreateProjectRequest request, CancellationToken cancellationToken)
    {
        var tenantId = _tenant.RequireTenantId();
        var business = await BusinessAccess.RequireAsync(_db, tenantId, businessId, cancellationToken);
        var location = await _db.Locations.AsNoTracking()
            .Where(l => l.BusinessId == businessId)
            .OrderBy(l => l.CreatedAtUtc)
            .Select(l => l.City ?? l.Name)
            .FirstOrDefaultAsync(cancellationToken);

        ContentGuard.Require(request.Name, request.ClientName, request.Description, request.Outcomes);
        var project = Project.Create(
            tenantId,
            businessId,
            request.Name,
            request.ClientName,
            request.Industry ?? business.IndustryCode,
            request.Location ?? location,
            request.Description,
            request.Outcomes,
            request.StartedOn,
            request.CompletedOn,
            ProjectMaps.ParsePermission(request.PermissionScope),
            ProjectMaps.ParseConfidentiality(request.Confidentiality));
        _db.Projects.Add(project);
        await _db.SaveChangesAsync(cancellationToken);
        return await ProjectComposer.LoadAsync(_db, businessId, project.Id, cancellationToken);
    }
}

public sealed class UpdateProjectHandler
{
    private readonly IAppDbContext _db;
    private readonly ITenantContext _tenant;

    public UpdateProjectHandler(IAppDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    public async Task<ProjectDetailResponse> Handle(Guid businessId, Guid projectId, UpdateProjectRequest request, CancellationToken cancellationToken)
    {
        var tenantId = _tenant.RequireTenantId();
        await BusinessAccess.RequireAsync(_db, tenantId, businessId, cancellationToken);
        var project = await ProjectComposer.RequireAsync(_db, businessId, projectId, cancellationToken);
        ContentGuard.Require(request.Name, request.ClientName, request.Description, request.Outcomes);
        project.Update(
            request.Name,
            request.ClientName,
            request.Industry,
            request.Location,
            request.Description,
            request.Outcomes,
            request.StartedOn,
            request.CompletedOn,
            ProjectMaps.ParsePermission(request.PermissionScope),
            ProjectMaps.ParseConfidentiality(request.Confidentiality),
            ProjectMaps.ParsePublication(request.PublicationStatus));
        await _db.SaveChangesAsync(cancellationToken);
        return await ProjectComposer.LoadAsync(_db, businessId, projectId, cancellationToken);
    }
}

public sealed class LinkProjectServiceHandler
{
    private readonly IAppDbContext _db;
    private readonly ITenantContext _tenant;

    public LinkProjectServiceHandler(IAppDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    public async Task<ProjectDetailResponse> Handle(Guid businessId, Guid projectId, LinkNamedRequest request, CancellationToken cancellationToken)
    {
        var tenantId = _tenant.RequireTenantId();
        await BusinessAccess.RequireAsync(_db, tenantId, businessId, cancellationToken);
        await ProjectComposer.RequireAsync(_db, businessId, projectId, cancellationToken);
        var service = await _db.Services.FirstOrDefaultAsync(s => s.Id == request.Id && s.BusinessId == businessId, cancellationToken)
            ?? throw AppException.NotFound("Service was not found.");
        if (!await _db.ProjectServices.AnyAsync(l => l.ProjectId == projectId && l.ServiceId == service.Id, cancellationToken))
        {
            _db.ProjectServices.Add(ProjectServiceLink.Link(tenantId, projectId, service.Id));
            await _db.SaveChangesAsync(cancellationToken);
        }

        return await ProjectComposer.LoadAsync(_db, businessId, projectId, cancellationToken);
    }
}

public sealed class LinkProjectBrandHandler
{
    private readonly IAppDbContext _db;
    private readonly ITenantContext _tenant;

    public LinkProjectBrandHandler(IAppDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    public async Task<ProjectDetailResponse> Handle(Guid businessId, Guid projectId, LinkNamedRequest request, CancellationToken cancellationToken)
    {
        var tenantId = _tenant.RequireTenantId();
        await BusinessAccess.RequireAsync(_db, tenantId, businessId, cancellationToken);
        await ProjectComposer.RequireAsync(_db, businessId, projectId, cancellationToken);
        var brand = await _db.BusinessBrands.FirstOrDefaultAsync(b => b.BrandId == request.Id && b.BusinessId == businessId, cancellationToken)
            ?? throw AppException.NotFound("Brand is not linked to this business.");
        if (!await _db.ProjectBrands.AnyAsync(l => l.ProjectId == projectId && l.BrandId == brand.BrandId, cancellationToken))
        {
            _db.ProjectBrands.Add(ProjectBrandLink.Link(tenantId, projectId, brand.BrandId));
            await _db.SaveChangesAsync(cancellationToken);
        }

        return await ProjectComposer.LoadAsync(_db, businessId, projectId, cancellationToken);
    }
}

public sealed class RegisterProjectMediaHandler
{
    private readonly IAppDbContext _db;
    private readonly ITenantContext _tenant;

    public RegisterProjectMediaHandler(IAppDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    public async Task<ProjectDetailResponse> Handle(Guid businessId, Guid projectId, RegisterMediaRequest request, CancellationToken cancellationToken)
    {
        var tenantId = _tenant.RequireTenantId();
        await BusinessAccess.RequireAsync(_db, tenantId, businessId, cancellationToken);
        await ProjectComposer.RequireAsync(_db, businessId, projectId, cancellationToken);
        var asset = MediaAsset.Register(tenantId, businessId, request.Label, ProjectMaps.ParseMedia(request.Kind), request.SourceUrl);
        _db.MediaAssets.Add(asset);
        _db.ProjectMedia.Add(ProjectMedia.Attach(tenantId, projectId, asset.Id));
        await _db.SaveChangesAsync(cancellationToken);
        return await ProjectComposer.LoadAsync(_db, businessId, projectId, cancellationToken);
    }
}

public sealed class GenerateProjectContentHandler
{
    private readonly IAppDbContext _db;
    private readonly ITenantContext _tenant;

    public GenerateProjectContentHandler(IAppDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    public async Task<ProjectDetailResponse> Handle(Guid businessId, Guid projectId, CancellationToken cancellationToken)
    {
        var tenantId = _tenant.RequireTenantId();
        await BusinessAccess.RequireAsync(_db, tenantId, businessId, cancellationToken);
        var project = await ProjectComposer.RequireAsync(_db, businessId, projectId, cancellationToken);
        var serviceIds = await _db.ProjectServices.Where(l => l.ProjectId == projectId).Select(l => l.ServiceId).ToListAsync(cancellationToken);
        var names = await _db.Services.Where(s => serviceIds.Contains(s.Id)).Select(s => s.Name).ToListAsync(cancellationToken);
        var pack = ContentItem.FromProject(tenantId, businessId, project.Id, $"{project.Name} content pack");
        _db.ContentItems.Add(pack);
        foreach (var (kind, title, body) in ProjectContentFactory.Build(project, names))
        {
            _db.ContentVariants.Add(ContentVariant.Draft(tenantId, pack.Id, kind, title, body));
        }

        await _db.SaveChangesAsync(cancellationToken);
        return await ProjectComposer.LoadAsync(_db, businessId, projectId, cancellationToken);
    }
}

public sealed class RequestContentApprovalHandler
{
    private readonly IAppDbContext _db;
    private readonly ITenantContext _tenant;

    public RequestContentApprovalHandler(IAppDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    public async Task<ProjectDetailResponse> Handle(Guid businessId, Guid projectId, Guid contentId, CancellationToken cancellationToken)
    {
        var tenantId = _tenant.RequireTenantId();
        await BusinessAccess.RequireAsync(_db, tenantId, businessId, cancellationToken);
        await ProjectComposer.RequireAsync(_db, businessId, projectId, cancellationToken);
        var item = await _db.ContentItems.FirstOrDefaultAsync(c => c.Id == contentId && c.ProjectId == projectId, cancellationToken)
            ?? throw AppException.NotFound("Content pack was not found.");
        try
        {
            item.RequestApproval();
        }
        catch (InvalidOperationException ex)
        {
            throw AppException.Validation(ex.Message);
        }

        _db.ApprovalRequests.Add(ApprovalRequest.OpenFor(tenantId, item.Id, "Review project variants against permission scope before any publish."));
        await _db.SaveChangesAsync(cancellationToken);
        return await ProjectComposer.LoadAsync(_db, businessId, projectId, cancellationToken);
    }
}

public sealed class DecideContentApprovalHandler
{
    private readonly IAppDbContext _db;
    private readonly ITenantContext _tenant;

    public DecideContentApprovalHandler(IAppDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    public async Task<ProjectDetailResponse> Handle(
        Guid businessId,
        Guid projectId,
        Guid approvalId,
        DecideApprovalRequest request,
        CancellationToken cancellationToken)
    {
        var tenantId = _tenant.RequireTenantId();
        await BusinessAccess.RequireAsync(_db, tenantId, businessId, cancellationToken);
        var project = await ProjectComposer.RequireAsync(_db, businessId, projectId, cancellationToken);
        var approval = await _db.ApprovalRequests.FirstOrDefaultAsync(a => a.Id == approvalId && a.Open, cancellationToken)
            ?? throw AppException.NotFound("Open approval was not found.");
        var item = await _db.ContentItems.FirstOrDefaultAsync(c => c.Id == approval.ContentItemId && c.ProjectId == projectId, cancellationToken)
            ?? throw AppException.NotFound("Content pack was not found.");
        var variants = await _db.ContentVariants.Where(v => v.ContentItemId == item.Id).ToListAsync(cancellationToken);

        if (request.Approve)
        {
            item.MarkApproved();
            foreach (var variant in variants)
            {
                variant.ApproveForScope(project.AllowsPublication(variant.Kind));
            }

            if (variants.All(v => v.Status != ContentItemStatus.Approved))
            {
                item.MarkHold("Permission scope blocked every variant. Nothing is eligible to publish.");
            }
        }
        else
        {
            item.MarkRejected(request.Note);
            foreach (var variant in variants)
            {
                variant.Reject();
            }
        }

        approval.Close();
        _db.ApprovalDecisions.Add(ApprovalDecision.Record(
            tenantId,
            approval.Id,
            request.Approve ? ApprovalDecisionKind.Approved : ApprovalDecisionKind.Rejected,
            request.Note));
        await _db.SaveChangesAsync(cancellationToken);
        return await ProjectComposer.LoadAsync(_db, businessId, projectId, cancellationToken);
    }
}

internal static class ProjectComposer
{
    public static async Task<Project> RequireAsync(IAppDbContext db, Guid businessId, Guid projectId, CancellationToken cancellationToken) =>
        await db.Projects.FirstOrDefaultAsync(p => p.Id == projectId && p.BusinessId == businessId, cancellationToken)
            ?? throw AppException.NotFound("Project was not found.");

    public static async Task<ProjectDetailResponse> LoadAsync(IAppDbContext db, Guid businessId, Guid projectId, CancellationToken cancellationToken)
    {
        var project = await RequireAsync(db, businessId, projectId, cancellationToken);
        var serviceIds = await db.ProjectServices.Where(l => l.ProjectId == projectId).Select(l => l.ServiceId).ToListAsync(cancellationToken);
        var brandIds = await db.ProjectBrands.Where(l => l.ProjectId == projectId).Select(l => l.BrandId).ToListAsync(cancellationToken);
        var mediaIds = await db.ProjectMedia.Where(l => l.ProjectId == projectId).Select(l => l.MediaAssetId).ToListAsync(cancellationToken);
        var services = await db.Services.Where(s => serviceIds.Contains(s.Id)).Select(s => s.Name).ToListAsync(cancellationToken);
        var brands = await db.Brands.Where(b => brandIds.Contains(b.Id)).Select(b => b.Name).ToListAsync(cancellationToken);
        var media = await db.MediaAssets.Where(m => mediaIds.Contains(m.Id))
            .Select(m => new MediaAssetResponse(m.Id, m.Label, m.Kind.ToString(), m.SourceUrl, m.Note))
            .ToListAsync(cancellationToken);
        var packs = await db.ContentItems.AsNoTracking()
            .Where(c => c.ProjectId == projectId)
            .OrderByDescending(c => c.UpdatedAtUtc)
            .ToListAsync(cancellationToken);
        var packIds = packs.Select(p => p.Id).ToList();
        var variants = await db.ContentVariants.AsNoTracking().Where(v => packIds.Contains(v.ContentItemId)).ToListAsync(cancellationToken);
        var approvals = await db.ApprovalRequests.AsNoTracking().Where(a => packIds.Contains(a.ContentItemId)).ToListAsync(cancellationToken);
        var decisions = await db.ApprovalDecisions.AsNoTracking()
            .Where(d => approvals.Select(a => a.Id).Contains(d.ApprovalRequestId))
            .ToListAsync(cancellationToken);

        return new ProjectDetailResponse(
            project.ToSummary(),
            project.Description,
            project.Outcomes,
            project.Industry,
            project.Location,
            project.StartedOn,
            project.CompletedOn,
            services,
            brands,
            media,
            packs.Select(pack => new ContentItemResponse(
                pack.Id,
                pack.ProjectId,
                pack.Title,
                pack.Status.ToString(),
                pack.SourceNote,
                variants.Where(v => v.ContentItemId == pack.Id)
                    .Select(v => new ContentVariantResponse(v.Id, v.Kind.ToString(), v.Title, v.Body, v.Status.ToString(), v.PublicationHold))
                    .ToList(),
                approvals.Where(a => a.ContentItemId == pack.Id)
                    .Select(a =>
                    {
                        var decision = decisions.Where(d => d.ApprovalRequestId == a.Id).OrderByDescending(d => d.CreatedAtUtc).FirstOrDefault();
                        return new ApprovalResponse(a.Id, a.ContentItemId, a.Reason, a.Open, decision?.Kind.ToString(), decision?.Note);
                    })
                    .ToList()))
                .ToList(),
            "No project information is published beyond the stored permission scope. WhatsApp variants are drafts only.");
    }
}
