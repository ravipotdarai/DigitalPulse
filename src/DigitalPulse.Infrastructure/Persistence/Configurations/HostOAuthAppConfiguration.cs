using DigitalPulse.Domain.Platforms;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DigitalPulse.Infrastructure.Persistence.Configurations;

public sealed class HostOAuthAppConfiguration : IEntityTypeConfiguration<HostOAuthApp>
{
    public void Configure(EntityTypeBuilder<HostOAuthApp> builder)
    {
        builder.ToTable("HostOAuthApp");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Provider).HasMaxLength(32).IsRequired();
        builder.Property(x => x.ClientId).HasMaxLength(256).IsRequired();
        builder.Property(x => x.ClientSecret).HasMaxLength(512).IsRequired();
        builder.HasIndex(x => x.Provider).IsUnique();
    }
}
