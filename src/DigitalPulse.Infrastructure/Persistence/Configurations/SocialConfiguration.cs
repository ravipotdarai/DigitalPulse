using DigitalPulse.Domain.Businesses;
using DigitalPulse.Domain.Social;
using DigitalPulse.Domain.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DigitalPulse.Infrastructure.Persistence.Configurations;

public sealed class SocialContentItemConfiguration : IEntityTypeConfiguration<SocialContentItem>
{
    public void Configure(EntityTypeBuilder<SocialContentItem> builder)
    {
        builder.ToTable("SocialContentItem");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.PlatformCode).HasMaxLength(32).IsRequired();
        builder.Property(x => x.Kind).HasConversion<string>().HasMaxLength(32);
        builder.Property(x => x.Title).HasMaxLength(160).IsRequired();
        builder.Property(x => x.Body).HasMaxLength(4000).IsRequired();
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(32);
        builder.Property(x => x.VerificationStatus).HasConversion<string>().HasMaxLength(32);
        builder.Property(x => x.VerificationDetail).HasMaxLength(500);
        builder.Property(x => x.LastPublishError).HasMaxLength(500);
        builder.HasIndex(x => new { x.BusinessId, x.UpdatedAtUtc });
        builder.HasOne<Tenant>().WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Business>().WithMany().HasForeignKey(x => x.BusinessId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class SocialMetricSnapshotConfiguration : IEntityTypeConfiguration<SocialMetricSnapshot>
{
    public void Configure(EntityTypeBuilder<SocialMetricSnapshot> builder)
    {
        builder.ToTable("SocialMetricSnapshot");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.PlatformCode).HasMaxLength(32).IsRequired();
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(32);
        builder.Property(x => x.Detail).HasMaxLength(500).IsRequired();
        builder.HasIndex(x => new { x.BusinessId, x.CapturedAtUtc });
        builder.HasOne<Tenant>().WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Business>().WithMany().HasForeignKey(x => x.BusinessId).OnDelete(DeleteBehavior.Restrict);
    }
}
