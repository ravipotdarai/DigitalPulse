using DigitalPulse.Domain.Ai;
using DigitalPulse.Domain.Businesses;
using DigitalPulse.Domain.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DigitalPulse.Infrastructure.Persistence.Configurations;

public sealed class KnowledgeEntryConfiguration : IEntityTypeConfiguration<KnowledgeEntry>
{
    public void Configure(EntityTypeBuilder<KnowledgeEntry> builder)
    {
        builder.ToTable("KnowledgeEntry");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Title).HasMaxLength(160).IsRequired();
        builder.Property(x => x.Body).HasMaxLength(4000).IsRequired();
        builder.Property(x => x.Kind).HasConversion<string>().HasMaxLength(32);
        builder.Property(x => x.SourceUrl).HasMaxLength(2048);
        builder.HasIndex(x => new { x.BusinessId, x.CreatedAtUtc });
        builder.HasOne<Tenant>().WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Business>().WithMany().HasForeignKey(x => x.BusinessId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class GraphNodeConfiguration : IEntityTypeConfiguration<GraphNode>
{
    public void Configure(EntityTypeBuilder<GraphNode> builder)
    {
        builder.ToTable("GraphNode");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Kind).HasConversion<string>().HasMaxLength(32);
        builder.Property(x => x.Label).HasMaxLength(160).IsRequired();
        builder.Property(x => x.SourceKey).HasMaxLength(160).IsRequired();
        builder.Property(x => x.Value).HasMaxLength(2000);
        builder.HasIndex(x => new { x.BusinessId, x.SourceKey }).IsUnique();
        builder.HasOne<Tenant>().WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Business>().WithMany().HasForeignKey(x => x.BusinessId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class GraphEdgeConfiguration : IEntityTypeConfiguration<GraphEdge>
{
    public void Configure(EntityTypeBuilder<GraphEdge> builder)
    {
        builder.ToTable("GraphEdge");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Relation).HasMaxLength(80).IsRequired();
        builder.HasIndex(x => new { x.BusinessId, x.FromNodeId, x.ToNodeId, x.Relation }).IsUnique();
        builder.HasOne<Tenant>().WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Business>().WithMany().HasForeignKey(x => x.BusinessId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class AiRunConfiguration : IEntityTypeConfiguration<AiRun>
{
    public void Configure(EntityTypeBuilder<AiRun> builder)
    {
        builder.ToTable("AiRun");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Agent).HasConversion<string>().HasMaxLength(32);
        builder.Property(x => x.Prompt).HasMaxLength(4000).IsRequired();
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(32);
        builder.Property(x => x.Confidence).HasConversion<string>().HasMaxLength(32);
        builder.Property(x => x.Output).HasMaxLength(4000).IsRequired();
        builder.Property(x => x.ProviderName).HasMaxLength(80).IsRequired();
        builder.Property(x => x.HoldReason).HasMaxLength(500).IsRequired();
        builder.HasIndex(x => new { x.BusinessId, x.CreatedAtUtc });
        builder.HasOne<Tenant>().WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Business>().WithMany().HasForeignKey(x => x.BusinessId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class AiEvaluationConfiguration : IEntityTypeConfiguration<AiEvaluation>
{
    public void Configure(EntityTypeBuilder<AiEvaluation> builder)
    {
        builder.ToTable("AiEvaluation");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Confidence).HasConversion<string>().HasMaxLength(32);
        builder.Property(x => x.Summary).HasMaxLength(500).IsRequired();
        builder.HasIndex(x => x.AiRunId).IsUnique();
        builder.HasOne<Tenant>().WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<AiRun>().WithMany().HasForeignKey(x => x.AiRunId).OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class AiAuditEventConfiguration : IEntityTypeConfiguration<AiAuditEvent>
{
    public void Configure(EntityTypeBuilder<AiAuditEvent> builder)
    {
        builder.ToTable("AiAuditEvent");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Stage).HasMaxLength(40).IsRequired();
        builder.Property(x => x.Detail).HasMaxLength(500).IsRequired();
        builder.HasIndex(x => new { x.AiRunId, x.CreatedAtUtc });
        builder.HasOne<Tenant>().WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<AiRun>().WithMany().HasForeignKey(x => x.AiRunId).OnDelete(DeleteBehavior.Cascade);
    }
}
