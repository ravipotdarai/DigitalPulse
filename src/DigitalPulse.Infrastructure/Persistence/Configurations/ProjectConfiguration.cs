using DigitalPulse.Domain.Businesses;
using DigitalPulse.Domain.Content;
using DigitalPulse.Domain.Projects;
using DigitalPulse.Domain.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DigitalPulse.Infrastructure.Persistence.Configurations;

public sealed class ProjectConfiguration : IEntityTypeConfiguration<Project>
{
    public void Configure(EntityTypeBuilder<Project> builder)
    {
        builder.ToTable("Project");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(160).IsRequired();
        builder.Property(x => x.ClientName).HasMaxLength(160);
        builder.Property(x => x.Industry).HasMaxLength(80);
        builder.Property(x => x.Location).HasMaxLength(160);
        builder.Property(x => x.Description).HasMaxLength(4000);
        builder.Property(x => x.Outcomes).HasMaxLength(2000);
        builder.Property(x => x.PermissionScope).HasConversion<string>().HasMaxLength(32);
        builder.Property(x => x.Confidentiality).HasConversion<string>().HasMaxLength(32);
        builder.Property(x => x.PublicationStatus).HasConversion<string>().HasMaxLength(32);
        builder.HasIndex(x => new { x.BusinessId, x.UpdatedAtUtc });
        builder.HasOne<Tenant>().WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Business>().WithMany().HasForeignKey(x => x.BusinessId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class ProjectServiceLinkConfiguration : IEntityTypeConfiguration<ProjectServiceLink>
{
    public void Configure(EntityTypeBuilder<ProjectServiceLink> builder)
    {
        builder.ToTable("ProjectService");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => new { x.ProjectId, x.ServiceId }).IsUnique();
        builder.HasOne<Tenant>().WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Project>().WithMany().HasForeignKey(x => x.ProjectId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<Service>().WithMany().HasForeignKey(x => x.ServiceId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class ProjectBrandLinkConfiguration : IEntityTypeConfiguration<ProjectBrandLink>
{
    public void Configure(EntityTypeBuilder<ProjectBrandLink> builder)
    {
        builder.ToTable("ProjectBrand");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => new { x.ProjectId, x.BrandId }).IsUnique();
        builder.HasOne<Tenant>().WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Project>().WithMany().HasForeignKey(x => x.ProjectId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<Brand>().WithMany().HasForeignKey(x => x.BrandId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class MediaAssetConfiguration : IEntityTypeConfiguration<MediaAsset>
{
    public void Configure(EntityTypeBuilder<MediaAsset> builder)
    {
        builder.ToTable("MediaAsset");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Label).HasMaxLength(160).IsRequired();
        builder.Property(x => x.Kind).HasConversion<string>().HasMaxLength(32);
        builder.Property(x => x.SourceUrl).HasMaxLength(2048);
        builder.Property(x => x.Note).HasMaxLength(500).IsRequired();
        builder.HasOne<Tenant>().WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Business>().WithMany().HasForeignKey(x => x.BusinessId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class ProjectMediaConfiguration : IEntityTypeConfiguration<ProjectMedia>
{
    public void Configure(EntityTypeBuilder<ProjectMedia> builder)
    {
        builder.ToTable("ProjectMedia");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => new { x.ProjectId, x.MediaAssetId }).IsUnique();
        builder.HasOne<Tenant>().WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Project>().WithMany().HasForeignKey(x => x.ProjectId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<MediaAsset>().WithMany().HasForeignKey(x => x.MediaAssetId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class ContentItemConfiguration : IEntityTypeConfiguration<ContentItem>
{
    public void Configure(EntityTypeBuilder<ContentItem> builder)
    {
        builder.ToTable("ContentItem");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Title).HasMaxLength(160).IsRequired();
        builder.Property(x => x.ContentTypeCode).HasMaxLength(32).IsRequired();
        builder.Property(x => x.Slug).HasMaxLength(80).IsRequired();
        builder.Property(x => x.Excerpt).HasMaxLength(500).IsRequired();
        builder.Property(x => x.Body).IsRequired();
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(32);
        builder.Property(x => x.Visibility).HasConversion<string>().HasMaxLength(16);
        builder.Property(x => x.CanonicalUrl).HasMaxLength(2048);
        builder.Property(x => x.SourceNote).HasMaxLength(500).IsRequired();
        builder.Property(x => x.RowVersion).IsRowVersion();
        builder.HasIndex(x => new { x.BusinessId, x.Slug }).IsUnique();
        builder.HasIndex(x => new { x.TenantId, x.BusinessId });
        builder.HasIndex(x => new { x.BusinessId, x.Status });
        builder.HasIndex(x => new { x.BusinessId, x.PublishedAtUtc });
        builder.HasIndex(x => new { x.ProjectId, x.UpdatedAtUtc });
        builder.HasOne<Tenant>().WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Business>().WithMany().HasForeignKey(x => x.BusinessId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Project>().WithMany().HasForeignKey(x => x.ProjectId).IsRequired(false).OnDelete(DeleteBehavior.SetNull);
        builder.HasOne<ContentType>().WithMany().HasForeignKey(x => x.ContentTypeId).IsRequired(false).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<MediaAsset>().WithMany().HasForeignKey(x => x.FeaturedMediaAssetId).IsRequired(false).OnDelete(DeleteBehavior.SetNull);
    }
}

public sealed class ContentVariantConfiguration : IEntityTypeConfiguration<ContentVariant>
{
    public void Configure(EntityTypeBuilder<ContentVariant> builder)
    {
        builder.ToTable("ContentVariant");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Kind).HasConversion<string>().HasMaxLength(40);
        builder.Property(x => x.Title).HasMaxLength(160).IsRequired();
        builder.Property(x => x.Body).IsRequired();
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(32);
        builder.Property(x => x.PublicationHold).HasMaxLength(500).IsRequired();
        builder.HasOne<Tenant>().WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ContentItem>().WithMany().HasForeignKey(x => x.ContentItemId).OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class ApprovalRequestConfiguration : IEntityTypeConfiguration<ApprovalRequest>
{
    public void Configure(EntityTypeBuilder<ApprovalRequest> builder)
    {
        builder.ToTable("ApprovalRequest");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Reason).HasMaxLength(500).IsRequired();
        builder.HasOne<Tenant>().WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ContentItem>().WithMany().HasForeignKey(x => x.ContentItemId).OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class ApprovalDecisionConfiguration : IEntityTypeConfiguration<ApprovalDecision>
{
    public void Configure(EntityTypeBuilder<ApprovalDecision> builder)
    {
        builder.ToTable("ApprovalDecision");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Kind).HasConversion<string>().HasMaxLength(32);
        builder.Property(x => x.Note).HasMaxLength(500).IsRequired();
        builder.HasOne<Tenant>().WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ApprovalRequest>().WithMany().HasForeignKey(x => x.ApprovalRequestId).OnDelete(DeleteBehavior.Cascade);
    }
}
