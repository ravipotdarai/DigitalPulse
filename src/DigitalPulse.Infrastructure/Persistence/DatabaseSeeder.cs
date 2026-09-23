using DigitalPulse.Domain.Billing;
using DigitalPulse.Domain.Tenancy;
using DigitalPulse.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DigitalPulse.Infrastructure.Persistence;

public static class DatabaseSeeder
{
    public static async Task SeedAsync(AppDbContext db, CancellationToken cancellationToken = default)
    {
        if (!await db.Plans.AnyAsync(cancellationToken))
        {
            db.Plans.AddRange(
                SubscriptionPlan.Create("STARTER", "Starter", 2999m, 1, false, 1),
                SubscriptionPlan.Create("GROWTH", "Growth", 6999m, 3, false, 2),
                SubscriptionPlan.Create("BUSINESS", "Business", 14999m, 10, false, 3),
                SubscriptionPlan.Create("AGENCY", "Agency", 29999m, 100, true, 4));
        }

        if (!await db.Tenants.AnyAsync(t => t.Type == TenantType.Platform, cancellationToken))
        {
            db.Tenants.Add(Tenant.CreatePlatform("Your Company"));
        }

        await db.SaveChangesAsync(cancellationToken);
    }
}
