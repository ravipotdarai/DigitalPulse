using DigitalPulse.Application.Abstractions;
using DigitalPulse.Domain.Billing;
using DigitalPulse.Domain.Businesses;
using DigitalPulse.Domain.Common;
using DigitalPulse.Domain.Identity;
using DigitalPulse.Domain.Tenancy;
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

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("dp");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        modelBuilder.Entity<TenantMembership>().HasQueryFilter(e =>
            _tenantContext == null || e.TenantId == _tenantContext.TenantId);
        modelBuilder.Entity<Business>().HasQueryFilter(e =>
            _tenantContext == null || e.TenantId == _tenantContext.TenantId);
        modelBuilder.Entity<BusinessLocation>().HasQueryFilter(e =>
            _tenantContext == null || e.TenantId == _tenantContext.TenantId);
        modelBuilder.Entity<Subscription>().HasQueryFilter(e =>
            _tenantContext == null || e.TenantId == _tenantContext.TenantId);
    }

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
