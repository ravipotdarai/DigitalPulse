using DigitalPulse.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace DigitalPulse.Infrastructure.Persistence;

public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer("Server=localhost,1433;Database=DigitalPulse;Trusted_Connection=True;Encrypt=False;TrustServerCertificate=True;Connection Timeout=30")
            .Options;
        return new AppDbContext(options, tenantContext: null);
    }
}
