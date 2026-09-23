namespace DigitalPulse.Domain.Common;

public abstract class TenantOwnedEntity : Entity
{
    public Guid TenantId { get; protected set; }
}
