using DigitalPulse.Domain.Billing;
using DigitalPulse.Domain.Businesses;
using DigitalPulse.Domain.Tenancy;
using Microsoft.EntityFrameworkCore;

namespace DigitalPulse.Infrastructure.Persistence;

public static class DatabaseSeeder
{
    public static async Task SeedAsync(AppDbContext db, CancellationToken cancellationToken = default)
    {
        if (!await db.Plans.AnyAsync(cancellationToken))
        {
            db.Plans.AddRange(
                SubscriptionPlan.Create("STARTER", "Starter", 2999m, 1, false, 1, 5, 2, 25, false, 0),
                SubscriptionPlan.Create("GROWTH", "Growth", 6999m, 3, false, 2, 15, 10, 150, true, 2000),
                SubscriptionPlan.Create("BUSINESS", "Business", 14999m, 10, false, 3, 50, 30, 750, true, 10000),
                SubscriptionPlan.Create("AGENCY", "Agency", 29999m, 100, true, 4, 500, 200, 10000, true, 50000));
        }

        if (!await db.Tenants.AnyAsync(t => t.Type == TenantType.Platform, cancellationToken))
        {
            db.Tenants.Add(Tenant.CreatePlatform("Your Company"));
        }

        if (!await db.Industries.AnyAsync(cancellationToken))
        {
            db.Industries.AddRange(
                Industry.Create("RETAIL", "Retail"),
                Industry.Create("HOSPITALITY", "Hospitality"),
                Industry.Create("HEALTH", "Health"),
                Industry.Create("PROFESSIONAL", "Professional services"),
                Industry.Create("MANUFACTURING", "Manufacturing"),
                Industry.Create("EDUCATION", "Education"),
                Industry.Create("REALESTATE", "Real estate"),
                Industry.Create("TECHNOLOGY", "Technology"),
                Industry.Create("OTHER", "Other"));
        }

        if (!await db.FactTypes.AnyAsync(cancellationToken))
        {
            db.FactTypes.AddRange(
                FactType.Create("LEGAL_NAME", "Legal name"),
                FactType.Create("GSTIN", "GSTIN"),
                FactType.Create("TAGLINE", "Tagline"),
                FactType.Create("CLAIM", "Public claim"),
                FactType.Create("HOURS", "Hours"),
                FactType.Create("PRICE_RANGE", "Price range"),
                FactType.Create("FOUNDED_YEAR", "Founded year"),
                FactType.Create("BRAND_VOICE", "Brand voice"));
        }

        await db.SaveChangesAsync(cancellationToken);
    }
}
