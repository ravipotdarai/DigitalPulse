using DigitalPulse.Domain.Billing;
using DigitalPulse.Domain.Businesses;
using DigitalPulse.Domain.Identity;
using DigitalPulse.Domain.Platforms;
using DigitalPulse.Domain.Scans;
using DigitalPulse.Domain.Tenancy;
using DigitalPulse.Domain.Website;
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
    DbSet<Industry> Industries { get; }
    DbSet<FactType> FactTypes { get; }
    DbSet<ContactPoint> ContactPoints { get; }
    DbSet<BusinessCategory> Categories { get; }
    DbSet<Service> Services { get; }
    DbSet<Brand> Brands { get; }
    DbSet<BusinessBrand> BusinessBrands { get; }
    DbSet<BusinessFact> Facts { get; }
    DbSet<Customer> Customers { get; }
    DbSet<CustomerContact> CustomerContacts { get; }
    DbSet<PlatformConnection> Connections { get; }
    DbSet<Scan> Scans { get; }
    DbSet<Finding> Findings { get; }
    DbSet<FindingEvidence> FindingEvidence { get; }
    DbSet<WebsiteSnapshot> WebsiteSnapshots { get; }
    DbSet<SearchObservation> SearchObservations { get; }

    Task<PlatformConnection?> FindConnectionByStateAsync(string state, CancellationToken cancellationToken);
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
