using DigitalPulse.Domain.Common;

namespace DigitalPulse.Domain.Businesses;

public enum FactStatus
{
    Draft = 0,
    Approved = 1,
    Restricted = 2
}

public sealed class FactType : Entity
{
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;

    private FactType() { }

    public static FactType Create(string code, string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        return new FactType
        {
            Code = code.Trim().ToUpperInvariant(),
            Name = name.Trim()
        };
    }
}

public sealed class BusinessFact : TenantOwnedEntity
{
    public Guid BusinessId { get; private set; }
    public string FactTypeCode { get; private set; } = string.Empty;
    public string Value { get; private set; } = string.Empty;
    public FactStatus Status { get; private set; } = FactStatus.Draft;

    private BusinessFact() { }

    public static BusinessFact Create(Guid tenantId, Guid businessId, string factTypeCode, string value, FactStatus status)
    {
        if (tenantId == Guid.Empty) throw new ArgumentException("Tenant is required.", nameof(tenantId));
        if (businessId == Guid.Empty) throw new ArgumentException("Business is required.", nameof(businessId));
        ArgumentException.ThrowIfNullOrWhiteSpace(factTypeCode);
        ArgumentException.ThrowIfNullOrWhiteSpace(value);

        return new BusinessFact
        {
            TenantId = tenantId,
            BusinessId = businessId,
            FactTypeCode = factTypeCode.Trim().ToUpperInvariant(),
            Value = value.Trim(),
            Status = status
        };
    }

    public void Update(string factTypeCode, string value, FactStatus status)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(factTypeCode);
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        FactTypeCode = factTypeCode.Trim().ToUpperInvariant();
        Value = value.Trim();
        Status = status;
        Touch();
    }

    public bool CanPublish => Status == FactStatus.Approved;
}

public sealed class Industry : Entity
{
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;

    private Industry() { }

    public static Industry Create(string code, string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        return new Industry
        {
            Code = code.Trim().ToUpperInvariant(),
            Name = name.Trim()
        };
    }
}
