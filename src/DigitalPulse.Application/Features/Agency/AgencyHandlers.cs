using DigitalPulse.Application.Abstractions;
using DigitalPulse.Application.Common;
using DigitalPulse.Contracts.Agency;
using DigitalPulse.Domain.Agency;
using DigitalPulse.Domain.Ai;
using DigitalPulse.Domain.Billing;
using DigitalPulse.Domain.Businesses;
using DigitalPulse.Domain.Monitoring;
using DigitalPulse.Domain.Scans;
using DigitalPulse.Domain.Tenancy;
using Microsoft.EntityFrameworkCore;

namespace DigitalPulse.Application.Features.Agency;

internal static class AgencyMaps
{
    public static AgencyClientResponse ToResponse(
        this AgencyClient client,
        Business business,
        int locationCount,
        int openFindingCount,
        DateTimeOffset? lastScanAtUtc) =>
        new(
            client.Id,
            client.BusinessId,
            client.TenantId,
            business.Name,
            business.Website,
            client.Status.ToString(),
            client.ContactName,
            client.ContactEmail,
            client.Notes,
            client.ExternalRef,
            locationCount,
            openFindingCount,
            lastScanAtUtc);

    public static WhiteLabelResponse ToResponse(this WhiteLabelProfile profile, bool entitled) =>
        new(
            profile.Id,
            profile.DisplayName,
            profile.SupportEmail,
            profile.SupportPhone,
            profile.PrimaryColor,
            profile.LogoUrl,
            profile.CustomDomain,
            profile.Enabled,
            entitled,
            profile.HoldReason);

    public static AgencyWorkflowResponse ToResponse(
        this AgencyWorkflow workflow,
        IReadOnlyList<AgencyWorkflowStep> steps) =>
        new(
            workflow.Id,
            workflow.ClientId,
            workflow.BusinessId,
            workflow.Kind.ToString(),
            workflow.Status.ToString(),
            workflow.CurrentStep,
            workflow.CurrentStepName,
            workflow.HoldReason,
            steps.OrderBy(s => s.Ordinal)
                .Select(s => new AgencyWorkflowStepResponse(s.Id, s.Ordinal, s.Name, s.Completed, s.Note))
                .ToList());

    public static AgencyReportResponse ToResponse(this AgencyReport report, IReadOnlyList<AgencyReportLine> lines) =>
        new(
            report.Id,
            report.Scope.ToString(),
            report.ClientId,
            report.BusinessId,
            report.Title,
            report.ObservedFact,
            report.Recommendation,
            report.AiInterpretation,
            report.CustomerDecision,
            report.HoldReason,
            lines.Select(l => new AgencyReportLineResponse(l.Id, l.BusinessId, l.Kind.ToString(), l.Body)).ToList());
}

internal static class AgencyAccess
{
    public static async Task<(Tenant Tenant, Subscription Subscription, SubscriptionPlan Plan)> RequireAgencyAsync(
        IAppDbContext db,
        Guid tenantId,
        CancellationToken cancellationToken)
    {
        var tenant = await db.Tenants.FirstAsync(t => t.Id == tenantId, cancellationToken);
        AgencyPolicy.EnsureAgencyTenant(tenant.Type);
        var subscription = await db.Subscriptions
            .Where(s => s.TenantId == tenantId)
            .OrderByDescending(s => s.UpdatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw AppException.Validation("Select the Agency plan before opening the agency workspace.");
        EntitlementRules.EnsureUsable(subscription.Status);
        var plan = await db.Plans.FirstAsync(p => p.Id == subscription.PlanId, cancellationToken);
        Subscription.EnsurePlanMatchesTenant(tenant.Type, plan);
        return (tenant, subscription, plan);
    }
}

internal static class AgencyStore
{
    public static async Task BackfillClientsAsync(IAppDbContext db, Guid tenantId, CancellationToken cancellationToken)
    {
        var businesses = await db.Businesses.Where(b => b.TenantId == tenantId).ToListAsync(cancellationToken);
        var existing = await db.AgencyClients.Where(c => c.TenantId == tenantId).Select(c => c.BusinessId).ToListAsync(cancellationToken);
        var added = 0;
        foreach (var business in businesses.Where(b => !existing.Contains(b.Id)))
        {
            db.AgencyClients.Add(AgencyClient.Enroll(tenantId, business.Id));
            added++;
        }

        if (added > 0)
        {
            await db.SaveChangesAsync(cancellationToken);
        }
    }

    public static async Task<AgencyWorkspaceResponse> LoadAsync(
        IAppDbContext db,
        Tenant tenant,
        SubscriptionPlan plan,
        CancellationToken cancellationToken)
    {
        await BackfillClientsAsync(db, tenant.Id, cancellationToken);

        var clients = await db.AgencyClients.AsNoTracking()
            .Where(c => c.TenantId == tenant.Id)
            .OrderBy(c => c.CreatedAtUtc)
            .ToListAsync(cancellationToken);
        var businesses = await db.Businesses.AsNoTracking()
            .Where(b => b.TenantId == tenant.Id)
            .ToDictionaryAsync(b => b.Id, cancellationToken);
        var locations = await db.Locations.AsNoTracking()
            .Where(l => l.TenantId == tenant.Id)
            .GroupBy(l => l.BusinessId)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Key, x => x.Count, cancellationToken);
        var findings = await db.Findings.AsNoTracking()
            .Where(f => f.TenantId == tenant.Id && f.Status == FindingStatus.Open)
            .GroupBy(f => f.BusinessId)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Key, x => x.Count, cancellationToken);
        var lastScans = await db.Scans.AsNoTracking()
            .Where(s => s.TenantId == tenant.Id && s.Status == ScanStatus.Completed)
            .GroupBy(s => s.BusinessId)
            .Select(g => new { g.Key, At = g.Max(s => s.CompletedAtUtc) })
            .ToDictionaryAsync(x => x.Key, x => x.At, cancellationToken);

        var clientResponses = clients
            .Where(c => businesses.ContainsKey(c.BusinessId))
            .Select(c => c.ToResponse(
                businesses[c.BusinessId],
                locations.GetValueOrDefault(c.BusinessId),
                findings.GetValueOrDefault(c.BusinessId),
                lastScans.GetValueOrDefault(c.BusinessId)))
            .ToList();

        var profile = await db.WhiteLabelProfiles.AsNoTracking()
            .FirstOrDefaultAsync(p => p.TenantId == tenant.Id, cancellationToken)
            ?? WhiteLabelProfile.Create(tenant.Id);

        var workflows = await db.AgencyWorkflows.AsNoTracking()
            .Where(w => w.TenantId == tenant.Id)
            .OrderByDescending(w => w.UpdatedAtUtc)
            .Take(20)
            .ToListAsync(cancellationToken);
        var steps = await db.AgencyWorkflowSteps.AsNoTracking()
            .Where(s => workflows.Select(w => w.Id).Contains(s.WorkflowId))
            .ToListAsync(cancellationToken);
        var reports = await db.AgencyReports.AsNoTracking()
            .Where(r => r.TenantId == tenant.Id)
            .OrderByDescending(r => r.CreatedAtUtc)
            .Take(20)
            .ToListAsync(cancellationToken);
        var lines = await db.AgencyReportLines.AsNoTracking()
            .Where(l => reports.Select(r => r.Id).Contains(l.ReportId))
            .ToListAsync(cancellationToken);

        var cap = BillingPolicy.BusinessCap(tenant.Type, plan);
        return new AgencyWorkspaceResponse(
            tenant.Name,
            tenant.Type.ToString(),
            plan.Name,
            cap,
            plan.WhiteLabel,
            "Agency clients are businesses on this tenant. Reports use stored work only. White-label hosting is not invented.",
            profile.ToResponse(plan.WhiteLabel),
            clientResponses,
            workflows.Select(w => w.ToResponse(steps.Where(s => s.WorkflowId == w.Id).ToList())).ToList(),
            reports.Select(r => r.ToResponse(lines.Where(l => l.ReportId == r.Id).ToList())).ToList());
    }
}

public sealed class GetAgencyWorkspaceHandler
{
    private readonly IAppDbContext _db;
    private readonly ITenantContext _tenant;

    public GetAgencyWorkspaceHandler(IAppDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    public async Task<AgencyWorkspaceResponse> Handle(CancellationToken cancellationToken)
    {
        var tenantId = _tenant.RequireTenantId();
        var (tenant, _, plan) = await AgencyAccess.RequireAgencyAsync(_db, tenantId, cancellationToken);
        return await AgencyStore.LoadAsync(_db, tenant, plan, cancellationToken);
    }
}

public sealed class CreateAgencyClientHandler
{
    private readonly IAppDbContext _db;
    private readonly ITenantContext _tenant;

    public CreateAgencyClientHandler(IAppDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    public async Task<AgencyWorkspaceResponse> Handle(CreateAgencyClientRequest request, CancellationToken cancellationToken)
    {
        var tenantId = _tenant.RequireTenantId();
        var (tenant, _, plan) = await AgencyAccess.RequireAgencyAsync(_db, tenantId, cancellationToken);
        var current = await _db.Businesses.CountAsync(b => b.TenantId == tenantId, cancellationToken);
        EntitlementRules.EnsureCanAddBusiness(tenant.Type, plan, current);

        AgencyClientStatus status;
        try
        {
            status = AgencyPolicy.ParseStatus(request.Status);
        }
        catch (ArgumentException ex)
        {
            throw AppException.Validation(ex.Message);
        }

        try
        {
            var business = Business.Create(tenantId, request.Name, request.Website);
            var client = AgencyClient.Enroll(
                tenantId,
                business.Id,
                status,
                request.ContactName,
                request.ContactEmail,
                request.Notes,
                request.ExternalRef);
            _db.Businesses.Add(business);
            _db.AgencyClients.Add(client);
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (ArgumentException ex)
        {
            throw AppException.Validation(ex.Message);
        }

        return await AgencyStore.LoadAsync(_db, tenant, plan, cancellationToken);
    }
}

public sealed class UpdateAgencyClientHandler
{
    private readonly IAppDbContext _db;
    private readonly ITenantContext _tenant;

    public UpdateAgencyClientHandler(IAppDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    public async Task<AgencyWorkspaceResponse> Handle(
        Guid clientId,
        UpdateAgencyClientRequest request,
        CancellationToken cancellationToken)
    {
        var tenantId = _tenant.RequireTenantId();
        var (tenant, _, plan) = await AgencyAccess.RequireAgencyAsync(_db, tenantId, cancellationToken);
        var client = await _db.AgencyClients.FirstOrDefaultAsync(c => c.Id == clientId && c.TenantId == tenantId, cancellationToken)
            ?? throw AppException.NotFound("Agency client was not found.");

        try
        {
            client.Update(
                AgencyPolicy.ParseStatus(request.Status),
                request.ContactName,
                request.ContactEmail,
                request.Notes,
                request.ExternalRef);
        }
        catch (ArgumentException ex)
        {
            throw AppException.Validation(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            throw AppException.Validation(ex.Message);
        }

        await _db.SaveChangesAsync(cancellationToken);
        return await AgencyStore.LoadAsync(_db, tenant, plan, cancellationToken);
    }
}

public sealed class UpdateWhiteLabelHandler
{
    private readonly IAppDbContext _db;
    private readonly ITenantContext _tenant;

    public UpdateWhiteLabelHandler(IAppDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    public async Task<AgencyWorkspaceResponse> Handle(UpdateWhiteLabelRequest request, CancellationToken cancellationToken)
    {
        var tenantId = _tenant.RequireTenantId();
        var (tenant, _, plan) = await AgencyAccess.RequireAgencyAsync(_db, tenantId, cancellationToken);
        try
        {
            EntitlementRules.EnsureCanUseWhiteLabel(plan);
        }
        catch (InvalidOperationException ex)
        {
            throw AppException.Validation(ex.Message);
        }

        var profile = await _db.WhiteLabelProfiles.FirstOrDefaultAsync(p => p.TenantId == tenantId, cancellationToken);
        if (profile is null)
        {
            profile = WhiteLabelProfile.Create(tenantId);
            _db.WhiteLabelProfiles.Add(profile);
        }

        try
        {
            profile.Apply(
                request.DisplayName,
                request.SupportEmail,
                request.SupportPhone,
                request.PrimaryColor,
                request.LogoUrl,
                request.CustomDomain,
                request.Enabled);
        }
        catch (ArgumentException ex)
        {
            throw AppException.Validation(ex.Message);
        }

        await _db.SaveChangesAsync(cancellationToken);
        return await AgencyStore.LoadAsync(_db, tenant, plan, cancellationToken);
    }
}

public sealed class StartAgencyWorkflowHandler
{
    private readonly IAppDbContext _db;
    private readonly ITenantContext _tenant;

    public StartAgencyWorkflowHandler(IAppDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    public async Task<AgencyWorkspaceResponse> Handle(StartAgencyWorkflowRequest request, CancellationToken cancellationToken)
    {
        var tenantId = _tenant.RequireTenantId();
        var (tenant, _, plan) = await AgencyAccess.RequireAgencyAsync(_db, tenantId, cancellationToken);

        AgencyWorkflowKind kind;
        try
        {
            kind = AgencyPolicy.ParseWorkflowKind(request.Kind);
        }
        catch (ArgumentException ex)
        {
            throw AppException.Validation(ex.Message);
        }

        Guid? clientId = null;
        Guid? businessId = null;
        if (kind != AgencyWorkflowKind.WhiteLabelReview)
        {
            var client = request.ClientId is { } id
                ? await _db.AgencyClients.FirstOrDefaultAsync(c => c.Id == id && c.TenantId == tenantId, cancellationToken)
                : await _db.AgencyClients.OrderBy(c => c.CreatedAtUtc).FirstOrDefaultAsync(c => c.TenantId == tenantId, cancellationToken);
            if (client is null)
            {
                throw AppException.Validation("Add a client business before starting this workflow.");
            }

            clientId = client.Id;
            businessId = client.BusinessId;
        }

        var workflow = AgencyWorkflow.Start(tenantId, kind, clientId, businessId);
        _db.AgencyWorkflows.Add(workflow);
        foreach (var step in workflow.CreateSteps())
        {
            _db.AgencyWorkflowSteps.Add(step);
        }

        await _db.SaveChangesAsync(cancellationToken);
        return await AgencyStore.LoadAsync(_db, tenant, plan, cancellationToken);
    }
}

public sealed class AdvanceAgencyWorkflowHandler
{
    private readonly IAppDbContext _db;
    private readonly ITenantContext _tenant;

    public AdvanceAgencyWorkflowHandler(IAppDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    public async Task<AgencyWorkspaceResponse> Handle(
        Guid workflowId,
        AdvanceAgencyWorkflowRequest request,
        CancellationToken cancellationToken)
    {
        var tenantId = _tenant.RequireTenantId();
        var (tenant, _, plan) = await AgencyAccess.RequireAgencyAsync(_db, tenantId, cancellationToken);
        var workflow = await _db.AgencyWorkflows.FirstOrDefaultAsync(w => w.Id == workflowId && w.TenantId == tenantId, cancellationToken)
            ?? throw AppException.NotFound("Agency workflow was not found.");
        var steps = await _db.AgencyWorkflowSteps
            .Where(s => s.WorkflowId == workflow.Id && s.TenantId == tenantId)
            .OrderBy(s => s.Ordinal)
            .ToListAsync(cancellationToken);

        var hold = request.Hold;
        var holdReason = request.HoldReason;
        if (!hold && workflow.Kind == AgencyWorkflowKind.WhiteLabelReview && workflow.CurrentStepName.Contains("domain", StringComparison.OrdinalIgnoreCase))
        {
            hold = true;
            holdReason = AgencyPolicy.CustomDomainHold;
        }

        if (!hold && workflow.Kind == AgencyWorkflowKind.PresenceAudit && workflow.BusinessId is { } businessId)
        {
            var hasEvidence = await _db.Scans.AnyAsync(
                s => s.TenantId == tenantId && s.BusinessId == businessId && s.Status == ScanStatus.Completed,
                cancellationToken);
            if (!hasEvidence)
            {
                hold = true;
                holdReason = "Presence audit stays held until a completed DigitalPulse Check exists for this client.";
            }
        }

        try
        {
            workflow.Advance(steps, request.Note, hold, holdReason);
        }
        catch (InvalidOperationException ex)
        {
            throw AppException.Validation(ex.Message);
        }

        await _db.SaveChangesAsync(cancellationToken);
        return await AgencyStore.LoadAsync(_db, tenant, plan, cancellationToken);
    }
}

public sealed class AssembleAgencyReportHandler
{
    private readonly IAppDbContext _db;
    private readonly ITenantContext _tenant;

    public AssembleAgencyReportHandler(IAppDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    public async Task<AgencyWorkspaceResponse> Handle(AssembleAgencyReportRequest request, CancellationToken cancellationToken)
    {
        var tenantId = _tenant.RequireTenantId();
        var (tenant, _, plan) = await AgencyAccess.RequireAgencyAsync(_db, tenantId, cancellationToken);
        await AgencyStore.BackfillClientsAsync(_db, tenantId, cancellationToken);

        AgencyReportScope scope;
        try
        {
            scope = AgencyPolicy.ParseScope(request.Scope);
        }
        catch (ArgumentException ex)
        {
            throw AppException.Validation(ex.Message);
        }

        AgencyClient? client = null;
        if (scope == AgencyReportScope.Client)
        {
            client = request.ClientId is { } id
                ? await _db.AgencyClients.FirstOrDefaultAsync(c => c.Id == id && c.TenantId == tenantId, cancellationToken)
                : null;
            if (client is null)
            {
                throw AppException.NotFound("Agency client was not found.");
            }
        }

        var report = await AssembleAsync(_db, tenant, scope, client, cancellationToken);
        _db.AgencyReports.Add(report.Report);
        foreach (var line in report.Lines)
        {
            _db.AgencyReportLines.Add(line);
        }

        await _db.SaveChangesAsync(cancellationToken);
        return await AgencyStore.LoadAsync(_db, tenant, plan, cancellationToken);
    }

    private static async Task<(AgencyReport Report, List<AgencyReportLine> Lines)> AssembleAsync(
        IAppDbContext db,
        Tenant tenant,
        AgencyReportScope scope,
        AgencyClient? client,
        CancellationToken cancellationToken)
    {
        if (scope == AgencyReportScope.Client && client is not null)
        {
            var business = await db.Businesses.AsNoTracking()
                .FirstAsync(b => b.Id == client.BusinessId && b.TenantId == tenant.Id, cancellationToken);
            var facts = await FactsForAsync(db, tenant.Id, client.BusinessId, cancellationToken);
            var observed = facts.ScanCount == 0
                ? $"No DigitalPulse Check has completed for {business.Name}. Live provider metrics were not invented."
                : $"{business.Name} has {facts.ScanCount} completed check(s) and {facts.OpenFindings} open stored finding(s).";
            var recommendation = facts.OpenFindings > 0
                ? "Work the open stored findings on this client before adding another market."
                : "Run DigitalPulse Check on this client when a new official connection exists.";
            var ai = AgencyPolicy.AiInterpretationHold;
            if (facts.AiCompleted)
            {
                ai = "An evidence-backed orchestrator run exists for this client. Interpretation still cites stored facts only.";
            }

            var report = AgencyReport.Assemble(
                tenant.Id,
                AgencyReportScope.Client,
                client.Id,
                client.BusinessId,
                $"{business.Name} agency report",
                observed,
                recommendation,
                ai,
                facts.ScanCount == 0
                    ? "Client report assembled from stored work. A completed check is still missing."
                    : "Client report assembled from stored work. Live provider metrics stay held.");
            var lines = new List<AgencyReportLine>
            {
                AgencyReportLine.Create(tenant.Id, report.Id, AgencyReportLineKind.ObservedFact, observed, client.BusinessId),
                AgencyReportLine.Create(tenant.Id, report.Id, AgencyReportLineKind.Recommendation, recommendation, client.BusinessId),
                AgencyReportLine.Create(tenant.Id, report.Id, AgencyReportLineKind.AiInterpretation, ai, client.BusinessId)
            };
            return (report, lines);
        }

        var clients = await db.AgencyClients.AsNoTracking()
            .Where(c => c.TenantId == tenant.Id)
            .ToListAsync(cancellationToken);
        var businesses = await db.Businesses.AsNoTracking()
            .Where(b => b.TenantId == tenant.Id)
            .ToDictionaryAsync(b => b.Id, cancellationToken);
        var linesOut = new List<AgencyReportLine>();
        var withScans = 0;
        foreach (var item in clients)
        {
            if (!businesses.TryGetValue(item.BusinessId, out var business))
            {
                continue;
            }

            var facts = await FactsForAsync(db, tenant.Id, item.BusinessId, cancellationToken);
            if (facts.ScanCount > 0)
            {
                withScans++;
            }

            linesOut.Add(AgencyReportLine.Create(
                tenant.Id,
                Guid.Empty,
                AgencyReportLineKind.ObservedFact,
                $"{business.Name}: {facts.ScanCount} completed check(s), {facts.OpenFindings} open finding(s), {facts.OpenAlerts} open alert(s).",
                item.BusinessId));
        }

        var observedPortfolio =
            $"Portfolio has {clients.Count} client business(es) on this Agency tenant. {withScans} have a completed DigitalPulse Check. Live provider metrics were not invented.";
        var recommendationPortfolio = clients.Count == 0
            ? "Add a client business. Agency clients are extra businesses on this tenant, not child tenants."
            : "Assemble a client-scoped report before mixing findings across businesses.";
        var reportPortfolio = AgencyReport.Assemble(
            tenant.Id,
            AgencyReportScope.Portfolio,
            null,
            null,
            $"{tenant.Name} portfolio report",
            observedPortfolio,
            recommendationPortfolio,
            AgencyPolicy.AiInterpretationHold,
            "Portfolio report lists each client separately. Facts from one client are not copied onto another.");

        var attached = new List<AgencyReportLine>
        {
            AgencyReportLine.Create(tenant.Id, reportPortfolio.Id, AgencyReportLineKind.ObservedFact, observedPortfolio),
            AgencyReportLine.Create(tenant.Id, reportPortfolio.Id, AgencyReportLineKind.Recommendation, recommendationPortfolio),
            AgencyReportLine.Create(tenant.Id, reportPortfolio.Id, AgencyReportLineKind.AiInterpretation, AgencyPolicy.AiInterpretationHold)
        };
        attached.AddRange(linesOut.Select(l =>
            AgencyReportLine.Create(tenant.Id, reportPortfolio.Id, l.Kind, l.Body, l.BusinessId)));
        return (reportPortfolio, attached);
    }

    private static async Task<(int ScanCount, int OpenFindings, int OpenAlerts, bool AiCompleted)> FactsForAsync(
        IAppDbContext db,
        Guid tenantId,
        Guid businessId,
        CancellationToken cancellationToken)
    {
        var scans = await db.Scans.CountAsync(
            s => s.TenantId == tenantId && s.BusinessId == businessId && s.Status == ScanStatus.Completed,
            cancellationToken);
        var findings = await db.Findings.CountAsync(
            f => f.TenantId == tenantId && f.BusinessId == businessId && f.Status == FindingStatus.Open,
            cancellationToken);
        var alerts = await db.MonitoringAlerts.CountAsync(
            a => a.TenantId == tenantId && a.BusinessId == businessId && a.Status == AlertStatus.Open,
            cancellationToken);
        var ai = await db.AiRuns.AnyAsync(
            r => r.TenantId == tenantId && r.BusinessId == businessId && r.Status == AiRunStatus.Completed,
            cancellationToken);
        return (scans, findings, alerts, ai);
    }
}

public sealed class RecordAgencyReportDecisionHandler
{
    private readonly IAppDbContext _db;
    private readonly ITenantContext _tenant;

    public RecordAgencyReportDecisionHandler(IAppDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    public async Task<AgencyWorkspaceResponse> Handle(
        Guid reportId,
        RecordAgencyDecisionRequest request,
        CancellationToken cancellationToken)
    {
        var tenantId = _tenant.RequireTenantId();
        var (tenant, _, plan) = await AgencyAccess.RequireAgencyAsync(_db, tenantId, cancellationToken);
        var report = await _db.AgencyReports.FirstOrDefaultAsync(r => r.Id == reportId && r.TenantId == tenantId, cancellationToken)
            ?? throw AppException.NotFound("Agency report was not found.");
        report.RecordDecision(request.Decision);
        _db.AgencyReportLines.Add(AgencyReportLine.Create(
            tenantId,
            report.Id,
            AgencyReportLineKind.CustomerDecision,
            request.Decision,
            report.BusinessId));
        await _db.SaveChangesAsync(cancellationToken);
        return await AgencyStore.LoadAsync(_db, tenant, plan, cancellationToken);
    }
}
