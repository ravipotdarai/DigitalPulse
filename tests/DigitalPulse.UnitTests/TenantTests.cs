using DigitalPulse.Domain.Tenancy;
using Xunit;

namespace DigitalPulse.UnitTests;

public sealed class TenantTests
{
    [Fact]
    public void Self_serve_cannot_create_platform_tenant()
    {
        Assert.Throws<InvalidOperationException>(() => Tenant.Create("Ops", TenantType.Platform));
    }

    [Fact]
    public void Direct_and_agency_tenants_can_be_created()
    {
        var direct = Tenant.Create("Acme", TenantType.Direct);
        var agency = Tenant.Create("Northwind Agency", TenantType.Agency);
        Assert.Equal(TenantType.Direct, direct.Type);
        Assert.Equal(TenantType.Agency, agency.Type);
    }
}
