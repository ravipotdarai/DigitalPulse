using DigitalPulse.Domain.Businesses;
using DigitalPulse.Domain.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DigitalPulse.Infrastructure.Persistence.Configurations;

public sealed class BusinessLocationConfiguration : IEntityTypeConfiguration<BusinessLocation>
{
    public void Configure(EntityTypeBuilder<BusinessLocation> builder)
    {
        builder.ToTable("BusinessLocation");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(160).IsRequired();
        builder.Property(x => x.AddressLine).HasMaxLength(240);
        builder.Property(x => x.City).HasMaxLength(120);
        builder.Property(x => x.Region).HasMaxLength(120);
        builder.Property(x => x.PostalCode).HasMaxLength(20);
        builder.Property(x => x.CountryCode).HasMaxLength(2).IsRequired();
        builder.HasOne<Business>().WithMany().HasForeignKey(x => x.BusinessId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Tenant>().WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => new { x.TenantId, x.BusinessId });
    }
}
