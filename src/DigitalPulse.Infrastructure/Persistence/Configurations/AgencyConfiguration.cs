using DigitalPulse.Domain.Agency;
using DigitalPulse.Domain.Businesses;
using DigitalPulse.Domain.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DigitalPulse.Infrastructure.Persistence.Configurations;

public sealed class AgencyClientConfiguration : IEntityTypeConfiguration<AgencyClient>
{
    public void Configure(EntityTypeBuilder<AgencyClient> builder)
    {
        builder.ToTable("AgencyClient");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(16);
        builder.Property(x => x.ContactName).HasMaxLength(160);
        builder.Property(x => x.ContactEmail).HasMaxLength(256);
        builder.Property(x => x.Notes).HasMaxLength(500);
        builder.Property(x => x.ExternalRef).HasMaxLength(80);
        builder.HasIndex(x => new { x.TenantId, x.BusinessId }).IsUnique();
        builder.HasOne<Tenant>().WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Business>().WithMany().HasForeignKey(x => x.BusinessId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class WhiteLabelProfileConfiguration : IEntityTypeConfiguration<WhiteLabelProfile>
{
    public void Configure(EntityTypeBuilder<WhiteLabelProfile> builder)
    {
        builder.ToTable("WhiteLabelProfile");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.DisplayName).HasMaxLength(160).IsRequired();
        builder.Property(x => x.SupportEmail).HasMaxLength(256);
        builder.Property(x => x.SupportPhone).HasMaxLength(40);
        builder.Property(x => x.PrimaryColor).HasMaxLength(7).IsRequired();
        builder.Property(x => x.LogoUrl).HasMaxLength(2048);
        builder.Property(x => x.CustomDomain).HasMaxLength(253);
        builder.Property(x => x.HoldReason).HasMaxLength(500).IsRequired();
        builder.HasIndex(x => x.TenantId).IsUnique();
        builder.HasOne<Tenant>().WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class AgencyWorkflowConfiguration : IEntityTypeConfiguration<AgencyWorkflow>
{
    public void Configure(EntityTypeBuilder<AgencyWorkflow> builder)
    {
        builder.ToTable("AgencyWorkflow");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Kind).HasConversion<string>().HasMaxLength(32);
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(16);
        builder.Property(x => x.CurrentStepName).HasMaxLength(160).IsRequired();
        builder.Property(x => x.HoldReason).HasMaxLength(500).IsRequired();
        builder.HasIndex(x => new { x.TenantId, x.UpdatedAtUtc });
        builder.HasOne<Tenant>().WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class AgencyWorkflowStepConfiguration : IEntityTypeConfiguration<AgencyWorkflowStep>
{
    public void Configure(EntityTypeBuilder<AgencyWorkflowStep> builder)
    {
        builder.ToTable("AgencyWorkflowStep");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(160).IsRequired();
        builder.Property(x => x.Note).HasMaxLength(500);
        builder.HasIndex(x => new { x.WorkflowId, x.Ordinal }).IsUnique();
        builder.HasOne<Tenant>().WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<AgencyWorkflow>().WithMany().HasForeignKey(x => x.WorkflowId).OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class AgencyReportConfiguration : IEntityTypeConfiguration<AgencyReport>
{
    public void Configure(EntityTypeBuilder<AgencyReport> builder)
    {
        builder.ToTable("AgencyReport");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Scope).HasConversion<string>().HasMaxLength(16);
        builder.Property(x => x.Title).HasMaxLength(200).IsRequired();
        builder.Property(x => x.ObservedFact).HasMaxLength(2000).IsRequired();
        builder.Property(x => x.Recommendation).HasMaxLength(2000).IsRequired();
        builder.Property(x => x.AiInterpretation).HasMaxLength(2000).IsRequired();
        builder.Property(x => x.CustomerDecision).HasMaxLength(500);
        builder.Property(x => x.HoldReason).HasMaxLength(500).IsRequired();
        builder.HasIndex(x => new { x.TenantId, x.CreatedAtUtc });
        builder.HasOne<Tenant>().WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class AgencyReportLineConfiguration : IEntityTypeConfiguration<AgencyReportLine>
{
    public void Configure(EntityTypeBuilder<AgencyReportLine> builder)
    {
        builder.ToTable("AgencyReportLine");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Kind).HasConversion<string>().HasMaxLength(24);
        builder.Property(x => x.Body).HasMaxLength(2000).IsRequired();
        builder.HasIndex(x => x.ReportId);
        builder.HasOne<Tenant>().WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<AgencyReport>().WithMany().HasForeignKey(x => x.ReportId).OnDelete(DeleteBehavior.Cascade);
    }
}
