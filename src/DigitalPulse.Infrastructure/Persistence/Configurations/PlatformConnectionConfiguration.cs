using DigitalPulse.Domain.Businesses;
using DigitalPulse.Domain.Platforms;
using DigitalPulse.Domain.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DigitalPulse.Infrastructure.Persistence.Configurations;

public sealed class PlatformConnectionConfiguration : IEntityTypeConfiguration<PlatformConnection>
{
    public void Configure(EntityTypeBuilder<PlatformConnection> builder)
    {
        builder.ToTable("PlatformConnection");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.PlatformCode).HasMaxLength(32).IsRequired();
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(32);
        builder.Property(x => x.AuthMode).HasConversion<string>().HasMaxLength(32);
        builder.Property(x => x.ExternalAccount).HasMaxLength(160);
        builder.Property(x => x.GrantKind).HasMaxLength(32);
        builder.Property(x => x.GrantReference).HasMaxLength(64);
        builder.Property(x => x.AuthorizationState).HasMaxLength(64);
        builder.Property(x => x.AccessToken).HasMaxLength(4000);
        builder.Property(x => x.RefreshToken).HasMaxLength(4000);
        builder.Property(x => x.TokenScope).HasMaxLength(500);
        builder.Property(x => x.LastHealthStatus).HasMaxLength(32);
        builder.Property(x => x.LastError).HasMaxLength(500);
        builder.HasIndex(x => new { x.BusinessId, x.PlatformCode }).IsUnique();
        builder.HasIndex(x => x.AuthorizationState);
        builder.HasOne<Tenant>().WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Business>().WithMany().HasForeignKey(x => x.BusinessId).OnDelete(DeleteBehavior.Restrict);
    }
}
