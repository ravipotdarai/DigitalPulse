using DigitalPulse.Domain.Businesses;
using DigitalPulse.Domain.Scans;
using DigitalPulse.Domain.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DigitalPulse.Infrastructure.Persistence.Configurations;

public sealed class ScanConfiguration : IEntityTypeConfiguration<Scan>
{
    public void Configure(EntityTypeBuilder<Scan> builder)
    {
        builder.ToTable("Scan");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Trigger).HasConversion<string>().HasMaxLength(32);
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(32);
        builder.Property(x => x.Summary).HasMaxLength(240);
        builder.Property(x => x.Error).HasMaxLength(500);
        builder.HasIndex(x => new { x.TenantId, x.StartedAtUtc });
        builder.HasIndex(x => new { x.BusinessId, x.StartedAtUtc });
        builder.HasOne<Tenant>().WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Business>().WithMany().HasForeignKey(x => x.BusinessId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class FindingConfiguration : IEntityTypeConfiguration<Finding>
{
    public void Configure(EntityTypeBuilder<Finding> builder)
    {
        builder.ToTable("Finding");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Category).HasMaxLength(32).IsRequired();
        builder.Property(x => x.Severity).HasConversion<string>().HasMaxLength(32);
        builder.Property(x => x.Title).HasMaxLength(160).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(1000).IsRequired();
        builder.Property(x => x.ExpectedValue).HasMaxLength(400);
        builder.Property(x => x.ObservedValue).HasMaxLength(400);
        builder.Property(x => x.Recommendation).HasMaxLength(500);
        builder.Property(x => x.SuggestedAction).HasMaxLength(500);
        builder.Property(x => x.VerificationMethod).HasMaxLength(400);
        builder.Property(x => x.AutomationState).HasConversion<string>().HasMaxLength(32);
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(32);
        builder.Property(x => x.ResolutionPath).HasMaxLength(32);
        builder.Property(x => x.PlaybookCode).HasMaxLength(64);
        builder.HasIndex(x => x.ScanId);
        builder.HasIndex(x => new { x.BusinessId, x.Status });
        builder.HasOne<Tenant>().WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Scan>().WithMany().HasForeignKey(x => x.ScanId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<Business>().WithMany().HasForeignKey(x => x.BusinessId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class FindingEvidenceConfiguration : IEntityTypeConfiguration<FindingEvidence>
{
    public void Configure(EntityTypeBuilder<FindingEvidence> builder)
    {
        builder.ToTable("FindingEvidence");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Kind).HasConversion<string>().HasMaxLength(32);
        builder.Property(x => x.Label).HasMaxLength(120).IsRequired();
        builder.Property(x => x.Value).HasMaxLength(500).IsRequired();
        builder.Property(x => x.Source).HasMaxLength(120).IsRequired();
        builder.HasIndex(x => x.FindingId);
        builder.HasOne<Tenant>().WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Finding>().WithMany().HasForeignKey(x => x.FindingId).OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class FindingStepConfiguration : IEntityTypeConfiguration<FindingStep>
{
    public void Configure(EntityTypeBuilder<FindingStep> builder)
    {
        builder.ToTable("FindingStep");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Title).HasMaxLength(160).IsRequired();
        builder.Property(x => x.Detail).HasMaxLength(1000).IsRequired();
        builder.Property(x => x.OfficialUrl).HasMaxLength(2048);
        builder.HasIndex(x => x.FindingId);
        builder.HasOne<Tenant>().WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Finding>().WithMany().HasForeignKey(x => x.FindingId).OnDelete(DeleteBehavior.Cascade);
    }
}
