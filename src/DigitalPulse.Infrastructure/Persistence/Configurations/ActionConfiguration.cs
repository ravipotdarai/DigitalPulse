using DigitalPulse.Domain.Actions;
using DigitalPulse.Domain.Businesses;
using DigitalPulse.Domain.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DigitalPulse.Infrastructure.Persistence.Configurations;

public sealed class AutomationPolicyConfiguration : IEntityTypeConfiguration<AutomationPolicy>
{
    public void Configure(EntityTypeBuilder<AutomationPolicy> builder)
    {
        builder.ToTable("AutomationPolicy");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Mode).HasConversion<string>().HasMaxLength(32);
        builder.HasIndex(x => x.TenantId).IsUnique();
        builder.HasOne<Tenant>().WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class WorkActionConfiguration : IEntityTypeConfiguration<WorkAction>
{
    public void Configure(EntityTypeBuilder<WorkAction> builder)
    {
        builder.ToTable("WorkAction");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Kind).HasConversion<string>().HasMaxLength(40);
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(32);
        builder.Property(x => x.Risk).HasConversion<string>().HasMaxLength(16);
        builder.Property(x => x.Title).HasMaxLength(160).IsRequired();
        builder.Property(x => x.IdempotencyKey).HasMaxLength(160).IsRequired();
        builder.Property(x => x.TargetLabel).HasMaxLength(160);
        builder.Property(x => x.HoldReason).HasMaxLength(500).IsRequired();
        builder.HasIndex(x => new { x.BusinessId, x.UpdatedAtUtc });
        builder.HasIndex(x => new { x.TenantId, x.IdempotencyKey, x.Status });
        builder.HasOne<Tenant>().WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Business>().WithMany().HasForeignKey(x => x.BusinessId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class ActionAttemptConfiguration : IEntityTypeConfiguration<ActionAttempt>
{
    public void Configure(EntityTypeBuilder<ActionAttempt> builder)
    {
        builder.ToTable("ActionAttempt");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Outcome).HasMaxLength(32).IsRequired();
        builder.Property(x => x.Detail).HasMaxLength(500).IsRequired();
        builder.HasIndex(x => new { x.WorkActionId, x.Ordinal }).IsUnique();
        builder.HasOne<Tenant>().WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<WorkAction>().WithMany().HasForeignKey(x => x.WorkActionId).OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class ActionVerificationConfiguration : IEntityTypeConfiguration<ActionVerification>
{
    public void Configure(EntityTypeBuilder<ActionVerification> builder)
    {
        builder.ToTable("ActionVerification");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(32);
        builder.Property(x => x.Detail).HasMaxLength(500).IsRequired();
        builder.HasIndex(x => new { x.WorkActionId, x.CreatedAtUtc });
        builder.HasOne<Tenant>().WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<WorkAction>().WithMany().HasForeignKey(x => x.WorkActionId).OnDelete(DeleteBehavior.Cascade);
    }
}
