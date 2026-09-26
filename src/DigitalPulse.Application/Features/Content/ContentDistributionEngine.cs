using DigitalPulse.Application.Abstractions;
using DigitalPulse.Application.Common;
using DigitalPulse.Application.Features.Identity;
using DigitalPulse.Domain.Billing;
using DigitalPulse.Domain.Content;
using DigitalPulse.Domain.Operations;
using DigitalPulse.Domain.Platforms;
using DigitalPulse.Domain.Projects;
using Microsoft.EntityFrameworkCore;

namespace DigitalPulse.Application.Features.Content;

internal static class ContentDistributionEngine
{
    public static readonly string[] Everywhere =
        ["HUB", "WEBSITE", "GOOGLE", "LINKEDIN", "FACEBOOK", "INSTAGRAM", "YOUTUBE"];

    public static string Normalize(string? providerCode) =>
        string.IsNullOrWhiteSpace(providerCode) ? "HUB" : providerCode.Trim().ToUpperInvariant();

    public static void RequireApproved(ContentItem item)
    {
        if (item.Status is not (ContentItemStatus.Approved or ContentItemStatus.Published or ContentItemStatus.Scheduled))
        {
            throw AppException.Validation("Approve the article before distribution.");
        }
    }

    public static async Task EnsureEntitlementAsync(IAppDbContext db, Guid tenantId, bool fanOut, CancellationToken cancellationToken)
    {
        var subscription = await db.Subscriptions.AsNoTracking()
            .FirstOrDefaultAsync(s => s.TenantId == tenantId && s.Status == SubscriptionStatus.Active, cancellationToken);
        if (subscription is null)
        {
            if (fanOut)
            {
                throw AppException.Validation("Complete plan selection before Publish Everywhere.");
            }

            return;
        }

        var plan = await db.Plans.AsNoTracking().FirstAsync(p => p.Id == subscription.PlanId, cancellationToken);
        var start = new DateTimeOffset(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1, 0, 0, 0, TimeSpan.Zero);
        var used = await db.ContentDistributions.CountAsync(d => d.TenantId == tenantId && d.CreatedAtUtc >= start, cancellationToken);
        try
        {
            EntitlementRules.EnsureCanRunAction(plan, used);
        }
        catch (InvalidOperationException ex)
        {
            throw AppException.Validation(ex.Message);
        }
    }

    public static string BuildKey(Guid contentId, string code, Guid? locationId, string? supplied)
    {
        if (!string.IsNullOrWhiteSpace(supplied))
        {
            var custom = $"{supplied.Trim()}:{code}:{(locationId is Guid id ? id.ToString("N") : "none")}";
            return Clip(custom);
        }

        return Clip($"{contentId:N}:{code}:{(locationId is Guid loc ? loc.ToString("N") : "none")}");
    }

    public static async Task<IReadOnlyList<Guid?>> ResolveLocationsAsync(
        IAppDbContext db,
        Guid businessId,
        string code,
        Guid? locationId,
        IReadOnlyList<Guid>? locationIds,
        CancellationToken cancellationToken)
    {
        var requested = (locationIds ?? []).Where(id => id != Guid.Empty).Distinct().ToList();
        if (locationId is Guid one && one != Guid.Empty && !requested.Contains(one))
        {
            requested.Add(one);
        }

        if (requested.Count > 0)
        {
            var owned = await db.Locations.AsNoTracking()
                .Where(location => location.BusinessId == businessId && requested.Contains(location.Id))
                .Select(location => location.Id)
                .ToListAsync(cancellationToken);
            if (owned.Count != requested.Count)
            {
                throw AppException.Validation("Each publishing location must belong to this business.");
            }

            return owned.Cast<Guid?>().ToList();
        }

        if (code != "GOOGLE")
        {
            return [null];
        }

        var all = await db.Locations.AsNoTracking()
            .Where(location => location.BusinessId == businessId)
            .Select(location => (Guid?)location.Id)
            .ToListAsync(cancellationToken);
        return all.Count == 0 ? [null] : all;
    }

    public static async Task PlaceAsync(
        IAppDbContext db,
        Guid tenantId,
        Guid businessId,
        ContentItem item,
        string providerCode,
        Guid? locationId,
        string? idempotencyKey,
        IPlatformAdapterCatalog? catalog,
        CancellationToken cancellationToken)
    {
        var code = Normalize(providerCode);
        var key = BuildKey(item.Id, code, locationId, idempotencyKey);
        var existing = await db.ContentDistributions.FirstOrDefaultAsync(
            row => row.BusinessId == businessId && row.IdempotencyKey == key,
            cancellationToken);
        if (existing is not null)
        {
            return;
        }

        var link = await db.Connections.FirstOrDefaultAsync(
            connection => connection.BusinessId == businessId && connection.PlatformCode == code,
            cancellationToken);
        var variantId = await MatchVariantAsync(db, item.Id, code, cancellationToken);
        var row = ContentDistribution.Start(tenantId, businessId, item.Id, code, variantId, link?.Id, locationId, key);
        await ApplyOutcomeAsync(row, item, link, code, db, catalog, cancellationToken);
        db.ContentDistributions.Add(row);
        db.OperationsAudits.Add(OperationsAudit.Record(
            tenantId,
            "content.distribute",
            $"{code} {row.Status} location {(locationId?.ToString("N") ?? "none")} attempts {row.AttemptCount}."));
    }

    public static async Task ApplyOutcomeAsync(
        ContentDistribution row,
        ContentItem item,
        PlatformConnection? link,
        string code,
        IAppDbContext db,
        IPlatformAdapterCatalog? catalog,
        CancellationToken cancellationToken)
    {
        if (code == "HUB")
        {
            if (item.Status != ContentItemStatus.Published)
            {
                item.Publish();
            }

            row.MarkPublished(item.Slug);
            return;
        }

        if (code == "WHATSAPP")
        {
            row.Hold("WhatsApp stays on consented Cloud API templates. Generic article posts are out of scope.");
            return;
        }

        var adapter = Find(catalog, code);
        var caps = adapter?.Describe().Capabilities;
        if (code == "WEBSITE" || caps is { AssistedOnly: true })
        {
            row.Hold(adapter?.Describe().Summary
                ?? "Customer website publish stays assisted until an official CMS write exists. DigitalPulse will not invent a CMS post.");
            return;
        }

        if (caps is { CanPublish: false })
        {
            row.Hold($"{code} does not expose an official publish in this catalog. Distribution stays assisted.");
            return;
        }

        if (link is null || !link.HasLiveCredential)
        {
            row.Hold("No live official credential for this provider. Distribution stays assisted — DigitalPulse will not invent a post.");
            return;
        }

        if (adapter is null)
        {
            row.Hold("Official write for long-form hub articles is not confirmed on this provider. Use Social compose for short posts.");
            return;
        }

        var body = await BodyForAsync(db, item, row, cancellationToken);
        var result = await adapter.PublishAsync(link, item.Title, body, cancellationToken);
        if (result.Status.Equals("Published", StringComparison.OrdinalIgnoreCase))
        {
            row.MarkPublished(result.Detail);
            return;
        }

        row.Hold(string.IsNullOrWhiteSpace(result.Detail)
            ? "Official provider did not confirm publication. DigitalPulse will not invent a post."
            : result.Detail);
    }

    public static IPlatformAdapter? Find(IPlatformAdapterCatalog? catalog, string code) =>
        catalog?.All().FirstOrDefault(adapter => string.Equals(adapter.Describe().Code, code, StringComparison.OrdinalIgnoreCase));

    public static async Task<ContentDistribution> RequireRowAsync(
        IAppDbContext db,
        Guid tenantId,
        Guid businessId,
        Guid contentId,
        Guid distributionId,
        CancellationToken cancellationToken)
    {
        await BusinessAccess.RequireAsync(db, tenantId, businessId, cancellationToken);
        var row = await db.ContentDistributions.FirstOrDefaultAsync(
            item => item.Id == distributionId && item.ContentItemId == contentId && item.BusinessId == businessId,
            cancellationToken) ?? throw AppException.NotFound("Distribution was not found.");
        if (row.LocationId is Guid locationId)
        {
            var owned = await db.Locations.AnyAsync(
                location => location.Id == locationId && location.BusinessId == businessId,
                cancellationToken);
            if (!owned)
            {
                throw AppException.Validation("This distribution location is not on the current business.");
            }
        }

        return row;
    }

    private static async Task<string> BodyForAsync(IAppDbContext db, ContentItem item, ContentDistribution row, CancellationToken cancellationToken)
    {
        if (row.ContentVariantId is Guid variantId)
        {
            var variant = await db.ContentVariants.AsNoTracking().FirstOrDefaultAsync(v => v.Id == variantId, cancellationToken);
            if (variant is not null && !string.IsNullOrWhiteSpace(variant.Body))
            {
                return variant.Body;
            }
        }

        var excerpt = string.IsNullOrWhiteSpace(item.Excerpt) ? item.Title : item.Excerpt;
        return excerpt.Length <= 240 ? excerpt : excerpt[..240];
    }

    private static async Task<Guid?> MatchVariantAsync(IAppDbContext db, Guid contentItemId, string code, CancellationToken cancellationToken)
    {
        var kind = code switch
        {
            "GOOGLE" => "GooglePost",
            "LINKEDIN" => "LinkedInPost",
            "FACEBOOK" => "FacebookPost",
            "INSTAGRAM" => "InstagramCaption",
            "YOUTUBE" => "YouTubeMetadata",
            "WEBSITE" => "WebsiteArticle",
            "WHATSAPP" => "WhatsAppTemplateDraft",
            "INDIAMART" => "IndiaMartContent",
            "JUSTDIAL" => "JustdialContent",
            _ => null
        };
        if (kind is null)
        {
            return null;
        }

        var variant = await db.ContentVariants.AsNoTracking()
            .FirstOrDefaultAsync(item => item.ContentItemId == contentItemId && item.Kind.ToString() == kind, cancellationToken);
        return variant?.Id;
    }

    private static string Clip(string value) => value.Length <= 80 ? value : value[..80];
}
