using DigitalPulse.Domain.Businesses;
using DigitalPulse.Domain.Directories;
using DigitalPulse.Domain.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DigitalPulse.Infrastructure.Persistence.Configurations;

public sealed class DirectoryTaskConfiguration : IEntityTypeConfiguration<DirectoryTask>
{
    public void Configure(EntityTypeBuilder<DirectoryTask> builder)
    {
        builder.ToTable("DirectoryTask");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.PlatformCode).HasMaxLength(32).IsRequired();
        builder.Property(x => x.Kind).HasConversion<string>().HasMaxLength(32);
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(32);
        builder.Property(x => x.PreparedName).HasMaxLength(160).IsRequired();
        builder.Property(x => x.PreparedPhone).HasMaxLength(64);
        builder.Property(x => x.PreparedWebsite).HasMaxLength(2048);
        builder.Property(x => x.PreparedCategory).HasMaxLength(160);
        builder.Property(x => x.PreparedServices).HasMaxLength(1000);
        builder.Property(x => x.VerificationNote).HasMaxLength(500);
        builder.Property(x => x.MonitorDetail).HasMaxLength(500);
        builder.Ignore(x => x.Steps);
        builder.HasIndex(x => new { x.BusinessId, x.UpdatedAtUtc });
        builder.HasOne<Tenant>().WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Business>().WithMany().HasForeignKey(x => x.BusinessId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class DirectoryStepConfiguration : IEntityTypeConfiguration<DirectoryStep>
{
    public void Configure(EntityTypeBuilder<DirectoryStep> builder)
    {
        builder.ToTable("DirectoryStep");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Title).HasMaxLength(160).IsRequired();
        builder.Property(x => x.Detail).HasMaxLength(500).IsRequired();
        builder.HasIndex(x => x.TaskId);
        builder.HasOne<Tenant>().WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<DirectoryTask>().WithMany().HasForeignKey(x => x.TaskId).OnDelete(DeleteBehavior.Cascade);
    }
}
