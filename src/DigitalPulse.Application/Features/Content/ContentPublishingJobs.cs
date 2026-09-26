using DigitalPulse.Application.Abstractions;
using DigitalPulse.Application.Common;
using DigitalPulse.Domain.Content;
using DigitalPulse.Domain.Operations;
using DigitalPulse.Domain.Projects;
using Microsoft.EntityFrameworkCore;

namespace DigitalPulse.Application.Features.Content;

public static class ContentPublishingJobs
{
    public const int MaxRetryAttempts = 5;

    public static async Task<int> ReleaseDueAsync(IAppDbContext db, Guid? businessId, bool ignoreFilters, CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var items = ignoreFilters ? db.ContentItems.IgnoreQueryFilters() : db.ContentItems;
        var query = items.Where(item => item.Status == ContentItemStatus.Scheduled && item.ScheduledAtUtc != null && item.ScheduledAtUtc <= now);
        if (businessId is Guid id)
        {
            query = query.Where(item => item.BusinessId == id);
        }

        var due = await query.ToListAsync(cancellationToken);
        var calendar = ignoreFilters ? db.ContentCalendar.IgnoreQueryFilters() : db.ContentCalendar;
        foreach (var item in due)
        {
            ContentGuard.Require(item.Title, item.Excerpt, item.Body);
            item.Publish();
            foreach (var entry in await calendar.Where(row => row.ContentItemId == item.Id && row.Status == "Scheduled").ToListAsync(cancellationToken))
            {
                entry.MarkPublished();
            }
        }

        return due.Count;
    }

    public static async Task<int> RetryHeldAsync(
        IAppDbContext db,
        IPlatformAdapterCatalog? catalog,
        Guid? businessId,
        bool ignoreFilters,
        CancellationToken cancellationToken)
    {
        var rows = ignoreFilters ? db.ContentDistributions.IgnoreQueryFilters() : db.ContentDistributions;
        var query = rows.Where(row => row.Status == ContentDistributionStatus.Failed && row.AttemptCount < MaxRetryAttempts);
        if (businessId is Guid id)
        {
            query = query.Where(row => row.BusinessId == id);
        }

        var held = await query.Take(40).ToListAsync(cancellationToken);
        var items = ignoreFilters ? db.ContentItems.IgnoreQueryFilters() : db.ContentItems;
        var connections = ignoreFilters ? db.Connections.IgnoreQueryFilters() : db.Connections;
        foreach (var row in held)
        {
            var item = await items.FirstOrDefaultAsync(content => content.Id == row.ContentItemId, cancellationToken);
            if (item is null)
            {
                continue;
            }

            try
            {
                row.Retry();
            }
            catch (InvalidOperationException)
            {
                continue;
            }

            var link = await connections.FirstOrDefaultAsync(
                connection => connection.BusinessId == row.BusinessId && connection.PlatformCode == row.ProviderCode,
                cancellationToken);
            await ContentDistributionEngine.ApplyOutcomeAsync(row, item, link, row.ProviderCode, db, catalog, cancellationToken);
            db.OperationsAudits.Add(OperationsAudit.Record(
                row.TenantId,
                "content.distribute.retry",
                $"worker {row.ProviderCode} attempts {row.AttemptCount} status {row.Status}."));
        }

        return held.Count;
    }

    public static async Task<int> VerifyConfirmedAsync(IAppDbContext db, Guid? businessId, bool ignoreFilters, CancellationToken cancellationToken)
    {
        var rows = ignoreFilters ? db.ContentDistributions.IgnoreQueryFilters() : db.ContentDistributions;
        var query = rows.Where(row =>
            row.Status == ContentDistributionStatus.Published &&
            row.ExternalContentId != null &&
            row.VerificationStatus != "Verified");
        if (businessId is Guid id)
        {
            query = query.Where(row => row.BusinessId == id);
        }

        var pending = await query.Take(40).ToListAsync(cancellationToken);
        foreach (var row in pending)
        {
            row.MarkVerified(row.ExternalContentId, "Official provider ID already stored.");
        }

        return pending.Count;
    }
}
