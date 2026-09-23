using DigitalPulse.Domain.Billing;
using DigitalPulse.Domain.Businesses;
using DigitalPulse.Domain.Identity;
using DigitalPulse.Domain.Tenancy;
using Microsoft.EntityFrameworkCore;

namespace DigitalPulse.Application.Abstractions;

public interface IAppDbContext
{
    DbSet<AppUser> Users { get; }
    DbSet<Tenant> Tenants { get; }
    DbSet<TenantMembership> Memberships { get; }
    DbSet<Business> Businesses { get; }
    DbSet<BusinessLocation> Locations { get; }
    DbSet<SubscriptionPlan> Plans { get; }
    DbSet<Subscription> Subscriptions { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
