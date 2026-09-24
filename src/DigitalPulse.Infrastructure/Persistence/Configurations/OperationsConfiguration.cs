using DigitalPulse.Domain.Operations;
using DigitalPulse.Domain.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DigitalPulse.Infrastructure.Persistence.Configurations;

public sealed class BackupSnapshotConfiguration : IEntityTypeConfiguration<BackupSnapshot>
{
    public void Configure(EntityTypeBuilder<BackupSnapshot> builder)
    {
        builder.ToTable("BackupSnapshot");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(16);
        builder.Property(x => x.Manifest).HasMaxLength(2000).IsRequired();
        builder.Property(x => x.Checksum).HasMaxLength(160).IsRequired();
        builder.Property(x => x.HoldReason).HasMaxLength(500).IsRequired();
        builder.HasIndex(x => new { x.TenantId, x.CreatedAtUtc });
        builder.HasOne<Tenant>().WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class RestoreAttemptConfiguration : IEntityTypeConfiguration<RestoreAttempt>
{
    public void Configure(EntityTypeBuilder<RestoreAttempt> builder)
    {
        builder.ToTable("RestoreAttempt");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(16);
        builder.Property(x => x.HoldReason).HasMaxLength(500).IsRequired();
        builder.HasIndex(x => new { x.TenantId, x.CreatedAtUtc });
        builder.HasOne<Tenant>().WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<BackupSnapshot>().WithMany().HasForeignKey(x => x.SnapshotId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class DisasterDrillConfiguration : IEntityTypeConfiguration<DisasterDrill>
{
    public void Configure(EntityTypeBuilder<DisasterDrill> builder)
    {
        builder.ToTable("DisasterDrill");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Kind).HasConversion<string>().HasMaxLength(16);
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(16);
        builder.Property(x => x.ObservedFact).HasMaxLength(500).IsRequired();
        builder.Property(x => x.HoldReason).HasMaxLength(500).IsRequired();
        builder.HasIndex(x => new { x.TenantId, x.CreatedAtUtc });
        builder.HasOne<Tenant>().WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class DependencyInventoryConfiguration : IEntityTypeConfiguration<DependencyInventory>
{
    public void Configure(EntityTypeBuilder<DependencyInventory> builder)
    {
        builder.ToTable("DependencyInventory");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Kind).HasConversion<string>().HasMaxLength(16);
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(16);
        builder.Property(x => x.Packages).HasMaxLength(2000).IsRequired();
        builder.Property(x => x.HoldReason).HasMaxLength(500).IsRequired();
        builder.HasIndex(x => new { x.TenantId, x.CreatedAtUtc });
        builder.HasOne<Tenant>().WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class ReadinessReviewConfiguration : IEntityTypeConfiguration<ReadinessReview>
{
    public void Configure(EntityTypeBuilder<ReadinessReview> builder)
    {
        builder.ToTable("ReadinessReview");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(24);
        builder.Property(x => x.EnvironmentName).HasMaxLength(32).IsRequired();
        builder.Property(x => x.HoldReason).HasMaxLength(500).IsRequired();
        builder.HasIndex(x => new { x.TenantId, x.CreatedAtUtc });
        builder.HasOne<Tenant>().WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class ReadinessCheckConfiguration : IEntityTypeConfiguration<ReadinessCheck>
{
    public void Configure(EntityTypeBuilder<ReadinessCheck> builder)
    {
        builder.ToTable("ReadinessCheck");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Code).HasMaxLength(40).IsRequired();
        builder.Property(x => x.Title).HasMaxLength(160).IsRequired();
        builder.Property(x => x.Outcome).HasConversion<string>().HasMaxLength(8);
        builder.Property(x => x.Detail).HasMaxLength(500).IsRequired();
        builder.HasIndex(x => x.ReviewId);
        builder.HasOne<Tenant>().WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ReadinessReview>().WithMany().HasForeignKey(x => x.ReviewId).OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class OperationsAuditConfiguration : IEntityTypeConfiguration<OperationsAudit>
{
    public void Configure(EntityTypeBuilder<OperationsAudit> builder)
    {
        builder.ToTable("OperationsAudit");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Action).HasMaxLength(40).IsRequired();
        builder.Property(x => x.Detail).HasMaxLength(500).IsRequired();
        builder.HasIndex(x => new { x.TenantId, x.CreatedAtUtc });
        builder.HasOne<Tenant>().WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
    }
}
