namespace DigitalPulse.Application.Abstractions;

public interface ITenantContext
{
    Guid? TenantId { get; }
    Guid RequireTenantId();
}
