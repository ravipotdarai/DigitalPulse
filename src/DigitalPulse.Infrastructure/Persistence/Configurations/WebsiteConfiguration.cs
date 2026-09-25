using DigitalPulse.Domain.Businesses;
using DigitalPulse.Domain.Tenancy;
using DigitalPulse.Domain.Website;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DigitalPulse.Infrastructure.Persistence.Configurations;

public sealed class WebsiteSnapshotConfiguration : IEntityTypeConfiguration<WebsiteSnapshot>
{
    public void Configure(EntityTypeBuilder<WebsiteSnapshot> builder)
    {
        builder.ToTable("WebsiteSnapshot");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Url).HasMaxLength(2048);
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(32);
        builder.Property(x => x.Title).HasMaxLength(240);
        builder.Property(x => x.MetaDescription).HasMaxLength(400);
        builder.Property(x => x.H1).HasMaxLength(240);
        builder.Property(x => x.CanonicalUrl).HasMaxLength(2048);
        builder.Property(x => x.Robots).HasMaxLength(160);
        builder.Property(x => x.Error).HasMaxLength(500);
        builder.Property(x => x.PageRole).HasConversion<string>().HasMaxLength(32);
        builder.HasIndex(x => new { x.BusinessId, x.FetchedAtUtc });
        builder.HasIndex(x => x.AuditRunId);
        builder.HasOne<Tenant>().WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Business>().WithMany().HasForeignKey(x => x.BusinessId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class SearchObservationConfiguration : IEntityTypeConfiguration<SearchObservation>
{
    public void Configure(EntityTypeBuilder<SearchObservation> builder)
    {
        builder.ToTable("SearchObservation");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Category).HasConversion<string>().HasMaxLength(32);
        builder.Property(x => x.Severity).HasConversion<string>().HasMaxLength(32);
        builder.Property(x => x.Title).HasMaxLength(160).IsRequired();
        builder.Property(x => x.Detail).HasMaxLength(1000).IsRequired();
        builder.Property(x => x.ExpectedValue).HasMaxLength(400);
        builder.Property(x => x.ObservedValue).HasMaxLength(400);
        builder.Property(x => x.Recommendation).HasMaxLength(500);
        builder.HasIndex(x => x.SnapshotId);
        builder.HasOne<Tenant>().WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Business>().WithMany().HasForeignKey(x => x.BusinessId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<WebsiteSnapshot>().WithMany().HasForeignKey(x => x.SnapshotId).OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class SearchConsoleQueryConfiguration : IEntityTypeConfiguration<SearchConsoleQuery>
{
    public void Configure(EntityTypeBuilder<SearchConsoleQuery> builder)
    {
        builder.ToTable("SearchConsoleQuery");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Query).HasMaxLength(400).IsRequired();
        builder.HasIndex(x => new { x.BusinessId, x.AuditRunId });
        builder.HasOne<Tenant>().WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Business>().WithMany().HasForeignKey(x => x.BusinessId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class TestReportConfiguration : IEntityTypeConfiguration<TestReport>
{
    public void Configure(EntityTypeBuilder<TestReport> builder)
    {
        builder.ToTable("TestReport");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Kind).HasConversion<string>().HasMaxLength(32);
        builder.Property(x => x.Title).HasMaxLength(160).IsRequired();
        builder.Property(x => x.ObservedFact).HasMaxLength(2000).IsRequired();
        builder.Property(x => x.Recommendation).HasMaxLength(1000);
        builder.Property(x => x.HoldReason).HasMaxLength(1000);
        builder.Property(x => x.Body).HasMaxLength(8000);
        builder.HasIndex(x => new { x.BusinessId, x.CreatedAtUtc });
        builder.HasOne<Tenant>().WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Business>().WithMany().HasForeignKey(x => x.BusinessId).OnDelete(DeleteBehavior.Restrict);
    }
}
