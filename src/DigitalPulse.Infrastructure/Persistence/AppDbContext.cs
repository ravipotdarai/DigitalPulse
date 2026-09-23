using DigitalPulse.Application.Abstractions;
using DigitalPulse.Domain.Billing;
using DigitalPulse.Domain.Businesses;
using DigitalPulse.Domain.Common;
using DigitalPulse.Domain.Identity;
using DigitalPulse.Domain.Platforms;
using DigitalPulse.Domain.Scans;
using DigitalPulse.Domain.Tenancy;
using DigitalPulse.Domain.Social;
using DigitalPulse.Domain.Website;
using Microsoft.EntityFrameworkCore;

namespace DigitalPulse.Infrastructure.Persistence;

public sealed class AppDbContext : DbContext, IAppDbContext
{
    private readonly ITenantContext? _tenantContext;

    public AppDbContext(DbContextOptions<AppDbContext> options, ITenantContext? tenantContext = null)
        : base(options)
    {
        _tenantContext = tenantContext;
    }

    public DbSet<AppUser> Users => Set<AppUser>();
    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<TenantMembership> Memberships => Set<TenantMembership>();
    public DbSet<Business> Businesses => Set<Business>();
    public DbSet<BusinessLocation> Locations => Set<BusinessLocation>();
    public DbSet<SubscriptionPlan> Plans => Set<SubscriptionPlan>();
    public DbSet<Subscription> Subscriptions => Set<Subscription>();
    public DbSet<Industry> Industries => Set<Industry>();
    public DbSet<FactType> FactTypes => Set<FactType>();
    public DbSet<ContactPoint> ContactPoints => Set<ContactPoint>();
    public DbSet<BusinessCategory> Categories => Set<BusinessCategory>();
    public DbSet<Service> Services => Set<Service>();
    public DbSet<Brand> Brands => Set<Brand>();
    public DbSet<BusinessBrand> BusinessBrands => Set<BusinessBrand>();
    public DbSet<BusinessFact> Facts => Set<BusinessFact>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<CustomerContact> CustomerContacts => Set<CustomerContact>();
    public DbSet<PlatformConnection> Connections => Set<PlatformConnection>();
    public DbSet<Scan> Scans => Set<Scan>();
    public DbSet<Finding> Findings => Set<Finding>();
    public DbSet<FindingEvidence> FindingEvidence => Set<FindingEvidence>();
    public DbSet<WebsiteSnapshot> WebsiteSnapshots => Set<WebsiteSnapshot>();
    public DbSet<SearchObservation> SearchObservations => Set<SearchObservation>();
    public DbSet<SocialContentItem> SocialContent => Set<SocialContentItem>();
    public DbSet<SocialMetricSnapshot> SocialMetrics => Set<SocialMetricSnapshot>();

    public Task<PlatformConnection?> FindConnectionByStateAsync(string state, CancellationToken cancellationToken) =>
        Connections.IgnoreQueryFilters().FirstOrDefaultAsync(c => c.AuthorizationState == state, cancellationToken);

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("dp");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        ApplyTenantFilter<TenantMembership>(modelBuilder);
        ApplyTenantFilter<Business>(modelBuilder);
        ApplyTenantFilter<BusinessLocation>(modelBuilder);
        ApplyTenantFilter<Subscription>(modelBuilder);
        ApplyTenantFilter<ContactPoint>(modelBuilder);
        ApplyTenantFilter<BusinessCategory>(modelBuilder);
        ApplyTenantFilter<Service>(modelBuilder);
        ApplyTenantFilter<Brand>(modelBuilder);
        ApplyTenantFilter<BusinessBrand>(modelBuilder);
        ApplyTenantFilter<BusinessFact>(modelBuilder);
        ApplyTenantFilter<Customer>(modelBuilder);
        ApplyTenantFilter<CustomerContact>(modelBuilder);
        ApplyTenantFilter<PlatformConnection>(modelBuilder);
        ApplyTenantFilter<Scan>(modelBuilder);
        ApplyTenantFilter<Finding>(modelBuilder);
        ApplyTenantFilter<FindingEvidence>(modelBuilder);
        ApplyTenantFilter<WebsiteSnapshot>(modelBuilder);
        ApplyTenantFilter<SearchObservation>(modelBuilder);
        ApplyTenantFilter<SocialContentItem>(modelBuilder);
        ApplyTenantFilter<SocialMetricSnapshot>(modelBuilder);
    }

    private void ApplyTenantFilter<TEntity>(ModelBuilder modelBuilder) where TEntity : TenantOwnedEntity =>
        modelBuilder.Entity<TEntity>().HasQueryFilter(e =>
            _tenantContext == null || e.TenantId == _tenantContext.TenantId);

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        foreach (var entry in ChangeTracker.Entries<Entity>())
        {
            if (entry.State == EntityState.Modified)
            {
                entry.Entity.Touch();
            }
        }

        return base.SaveChangesAsync(cancellationToken);
    }
}
