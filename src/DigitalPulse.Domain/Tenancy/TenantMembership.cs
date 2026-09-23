using DigitalPulse.Domain.Common;

namespace DigitalPulse.Domain.Tenancy;

public sealed class TenantMembership : TenantOwnedEntity
{
    public Guid UserId { get; private set; }
    public MembershipRole Role { get; private set; }

    private TenantMembership() { }

    public static TenantMembership CreateOwner(Guid tenantId, Guid userId)
    {
        if (tenantId == Guid.Empty) throw new ArgumentException("Tenant is required.", nameof(tenantId));
        if (userId == Guid.Empty) throw new ArgumentException("User is required.", nameof(userId));

        return new TenantMembership
        {
            TenantId = tenantId,
            UserId = userId,
            Role = MembershipRole.Owner
        };
    }
}
