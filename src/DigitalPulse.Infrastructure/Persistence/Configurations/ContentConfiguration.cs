using DigitalPulse.Domain.Businesses;
using DigitalPulse.Domain.Content;
using DigitalPulse.Domain.Projects;
using DigitalPulse.Domain.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DigitalPulse.Infrastructure.Persistence.Configurations;

public sealed class ContentTypeConfiguration : IEntityTypeConfiguration<ContentType>
{
    public void Configure(EntityTypeBuilder<ContentType> builder)
    {
        builder.ToTable("ContentType");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Code).HasMaxLength(32).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(80).IsRequired();
        builder.HasIndex(x => x.Code).IsUnique();
    }
}

public sealed class ContentCategoryConfiguration : IEntityTypeConfiguration<ContentCategory>
{
    public void Configure(EntityTypeBuilder<ContentCategory> builder)
    {
        builder.ToTable("ContentCategory");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(80).IsRequired();
        builder.Property(x => x.Slug).HasMaxLength(80).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(500).IsRequired();
        builder.HasIndex(x => new { x.BusinessId, x.Slug }).IsUnique();
        builder.HasOne<Tenant>().WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Business>().WithMany().HasForeignKey(x => x.BusinessId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class ContentTagConfiguration : IEntityTypeConfiguration<ContentTag>
{
    public void Configure(EntityTypeBuilder<ContentTag> builder)
    {
        builder.ToTable("ContentTag");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(80).IsRequired();
        builder.Property(x => x.Slug).HasMaxLength(80).IsRequired();
        builder.HasIndex(x => new { x.BusinessId, x.Slug }).IsUnique();
        builder.HasOne<Tenant>().WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Business>().WithMany().HasForeignKey(x => x.BusinessId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class ContentItemCategoryConfiguration : IEntityTypeConfiguration<ContentItemCategory>
{
    public void Configure(EntityTypeBuilder<ContentItemCategory> builder)
    {
        builder.ToTable("ContentItemCategory");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => new { x.ContentItemId, x.ContentCategoryId }).IsUnique();
        builder.HasOne<Tenant>().WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ContentItem>().WithMany().HasForeignKey(x => x.ContentItemId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<ContentCategory>().WithMany().HasForeignKey(x => x.ContentCategoryId).OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class ContentItemTagConfiguration : IEntityTypeConfiguration<ContentItemTag>
{
    public void Configure(EntityTypeBuilder<ContentItemTag> builder)
    {
        builder.ToTable("ContentItemTag");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => new { x.ContentItemId, x.ContentTagId }).IsUnique();
        builder.HasOne<Tenant>().WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ContentItem>().WithMany().HasForeignKey(x => x.ContentItemId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<ContentTag>().WithMany().HasForeignKey(x => x.ContentTagId).OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class ContentRevisionConfiguration : IEntityTypeConfiguration<ContentRevision>
{
    public void Configure(EntityTypeBuilder<ContentRevision> builder)
    {
        builder.ToTable("ContentRevision");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Title).HasMaxLength(160).IsRequired();
        builder.Property(x => x.Excerpt).HasMaxLength(500).IsRequired();
        builder.Property(x => x.Body).IsRequired();
        builder.Property(x => x.ChangeSummary).HasMaxLength(500).IsRequired();
        builder.HasIndex(x => new { x.ContentItemId, x.VersionNumber }).IsUnique();
        builder.HasOne<Tenant>().WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ContentItem>().WithMany().HasForeignKey(x => x.ContentItemId).OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class ContentItemMediaConfiguration : IEntityTypeConfiguration<ContentItemMedia>
{
    public void Configure(EntityTypeBuilder<ContentItemMedia> builder)
    {
        builder.ToTable("ContentItemMedia");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Role).HasConversion<string>().HasMaxLength(24);
        builder.HasIndex(x => new { x.ContentItemId, x.MediaAssetId, x.Role }).IsUnique();
        builder.HasOne<Tenant>().WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ContentItem>().WithMany().HasForeignKey(x => x.ContentItemId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<MediaAsset>().WithMany().HasForeignKey(x => x.MediaAssetId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class ContentSeoAnalysisConfiguration : IEntityTypeConfiguration<ContentSeoAnalysis>
{
    public void Configure(EntityTypeBuilder<ContentSeoAnalysis> builder)
    {
        builder.ToTable("ContentSeoAnalysis");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.FocusKeyword).HasMaxLength(80);
        builder.Property(x => x.SearchIntent).HasMaxLength(32).IsRequired();
        builder.Property(x => x.MetaTitle).HasMaxLength(80).IsRequired();
        builder.Property(x => x.MetaDescription).HasMaxLength(200).IsRequired();
        builder.Property(x => x.CanonicalUrl).HasMaxLength(2048);
        builder.Property(x => x.NotesJson).IsRequired();
        builder.HasIndex(x => x.ContentItemId);
        builder.HasOne<Tenant>().WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ContentItem>().WithMany().HasForeignKey(x => x.ContentItemId).OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class ContentTopicConfiguration : IEntityTypeConfiguration<ContentTopic>
{
    public void Configure(EntityTypeBuilder<ContentTopic> builder)
    {
        builder.ToTable("ContentTopic");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Topic).HasMaxLength(160).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(1000).IsRequired();
        builder.Property(x => x.SearchIntent).HasMaxLength(32).IsRequired();
        builder.Property(x => x.Status).HasMaxLength(24).IsRequired();
        builder.HasIndex(x => new { x.BusinessId, x.Topic });
        builder.HasOne<Tenant>().WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Business>().WithMany().HasForeignKey(x => x.BusinessId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class ContentOpportunityConfiguration : IEntityTypeConfiguration<ContentOpportunity>
{
    public void Configure(EntityTypeBuilder<ContentOpportunity> builder)
    {
        builder.ToTable("ContentOpportunity");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.SourceType).HasConversion<string>().HasMaxLength(32);
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(24);
        builder.Property(x => x.Reason).HasMaxLength(500).IsRequired();
        builder.HasIndex(x => new { x.BusinessId, x.Status });
        builder.HasOne<Tenant>().WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Business>().WithMany().HasForeignKey(x => x.BusinessId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ContentTopic>().WithMany().HasForeignKey(x => x.ContentTopicId).OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class ContentCalendarEntryConfiguration : IEntityTypeConfiguration<ContentCalendarEntry>
{
    public void Configure(EntityTypeBuilder<ContentCalendarEntry> builder)
    {
        builder.ToTable("ContentCalendarEntry");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Status).HasMaxLength(24).IsRequired();
        builder.Property(x => x.Channel).HasMaxLength(32).IsRequired();
        builder.HasIndex(x => new { x.BusinessId, x.ScheduledAtUtc });
        builder.HasOne<Tenant>().WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ContentItem>().WithMany().HasForeignKey(x => x.ContentItemId).OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class ContentDistributionConfiguration : IEntityTypeConfiguration<ContentDistribution>
{
    public void Configure(EntityTypeBuilder<ContentDistribution> builder)
    {
        builder.ToTable("ContentDistribution");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.ProviderCode).HasMaxLength(32).IsRequired();
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(32);
        builder.Property(x => x.ExternalContentId).HasMaxLength(160);
        builder.Property(x => x.FailureReason).HasMaxLength(500);
        builder.HasIndex(x => new { x.ContentItemId, x.ProviderCode });
        builder.HasOne<Tenant>().WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ContentItem>().WithMany().HasForeignKey(x => x.ContentItemId).OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class ContentMetricConfiguration : IEntityTypeConfiguration<ContentMetric>
{
    public void Configure(EntityTypeBuilder<ContentMetric> builder)
    {
        builder.ToTable("ContentMetric");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.ProviderCode).HasMaxLength(32).IsRequired();
        builder.Property(x => x.Detail).HasMaxLength(500).IsRequired();
        builder.HasIndex(x => new { x.ContentItemId, x.ProviderCode, x.MetricDate });
        builder.HasOne<Tenant>().WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ContentItem>().WithMany().HasForeignKey(x => x.ContentItemId).OnDelete(DeleteBehavior.Cascade);
    }
}
