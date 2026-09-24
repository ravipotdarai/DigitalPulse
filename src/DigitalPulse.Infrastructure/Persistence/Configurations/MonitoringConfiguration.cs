using DigitalPulse.Domain.Businesses;
using DigitalPulse.Domain.Monitoring;
using DigitalPulse.Domain.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DigitalPulse.Infrastructure.Persistence.Configurations;

public sealed class MonitoringScheduleConfiguration : IEntityTypeConfiguration<MonitoringSchedule>
{
    public void Configure(EntityTypeBuilder<MonitoringSchedule> builder)
    {
        builder.ToTable("MonitoringSchedule");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.HoldReason).HasMaxLength(500).IsRequired();
        builder.HasIndex(x => x.BusinessId).IsUnique();
        builder.HasIndex(x => x.NextRunAtUtc);
        builder.HasOne<Tenant>().WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Business>().WithMany().HasForeignKey(x => x.BusinessId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class MonitoringRunConfiguration : IEntityTypeConfiguration<MonitoringRun>
{
    public void Configure(EntityTypeBuilder<MonitoringRun> builder)
    {
        builder.ToTable("MonitoringRun");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Trigger).HasConversion<string>().HasMaxLength(16);
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(16);
        builder.Property(x => x.Summary).HasMaxLength(500).IsRequired();
        builder.HasIndex(x => new { x.BusinessId, x.StartedAtUtc });
        builder.HasOne<Tenant>().WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class MonitoringResultConfiguration : IEntityTypeConfiguration<MonitoringResult>
{
    public void Configure(EntityTypeBuilder<MonitoringResult> builder)
    {
        builder.ToTable("MonitoringResult");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Kind).HasConversion<string>().HasMaxLength(40);
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(16);
        builder.Property(x => x.Title).HasMaxLength(160).IsRequired();
        builder.Property(x => x.ObservedFact).HasMaxLength(500).IsRequired();
        builder.Property(x => x.Recommendation).HasMaxLength(500).IsRequired();
        builder.Property(x => x.PreviousValue).HasMaxLength(160);
        builder.Property(x => x.CurrentValue).HasMaxLength(160);
        builder.HasIndex(x => new { x.RunId, x.Kind });
        builder.HasOne<Tenant>().WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<MonitoringRun>().WithMany().HasForeignKey(x => x.RunId).OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class MonitoringAlertConfiguration : IEntityTypeConfiguration<MonitoringAlert>
{
    public void Configure(EntityTypeBuilder<MonitoringAlert> builder)
    {
        builder.ToTable("MonitoringAlert");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Severity).HasConversion<string>().HasMaxLength(16);
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(16);
        builder.Property(x => x.Title).HasMaxLength(160).IsRequired();
        builder.Property(x => x.Detail).HasMaxLength(500).IsRequired();
        builder.HasIndex(x => new { x.BusinessId, x.Status, x.OpenedAtUtc });
        builder.HasOne<Tenant>().WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class CompetitorConfiguration : IEntityTypeConfiguration<Competitor>
{
    public void Configure(EntityTypeBuilder<Competitor> builder)
    {
        builder.ToTable("Competitor");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(160).IsRequired();
        builder.Property(x => x.Website).HasMaxLength(2048);
        builder.Property(x => x.Notes).HasMaxLength(500);
        builder.HasIndex(x => new { x.BusinessId, x.Name }).IsUnique();
        builder.HasOne<Tenant>().WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class CompetitorObservationConfiguration : IEntityTypeConfiguration<CompetitorObservation>
{
    public void Configure(EntityTypeBuilder<CompetitorObservation> builder)
    {
        builder.ToTable("CompetitorObservation");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(16);
        builder.Property(x => x.Detail).HasMaxLength(500).IsRequired();
        builder.HasIndex(x => new { x.CompetitorId, x.RunId });
        builder.HasOne<Tenant>().WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Competitor>().WithMany().HasForeignKey(x => x.CompetitorId).OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class PresenceReportConfiguration : IEntityTypeConfiguration<PresenceReport>
{
    public void Configure(EntityTypeBuilder<PresenceReport> builder)
    {
        builder.ToTable("PresenceReport");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Kind).HasConversion<string>().HasMaxLength(16);
        builder.Property(x => x.Title).HasMaxLength(160).IsRequired();
        builder.Property(x => x.ObservedFact).HasMaxLength(4000).IsRequired();
        builder.Property(x => x.Recommendation).HasMaxLength(4000).IsRequired();
        builder.Property(x => x.AiInterpretation).HasMaxLength(4000).IsRequired();
        builder.Property(x => x.CustomerDecision).HasMaxLength(500);
        builder.Property(x => x.HoldReason).HasMaxLength(500).IsRequired();
        builder.HasIndex(x => new { x.BusinessId, x.CreatedAtUtc });
        builder.HasOne<Tenant>().WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
    }
}
