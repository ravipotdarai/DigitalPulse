using DigitalPulse.Domain.Common;

namespace DigitalPulse.Domain.Businesses;

public enum CustomerContactKind
{
    Mobile = 1,
    Email = 2
}

public sealed class Customer : TenantOwnedEntity
{
    public Guid BusinessId { get; private set; }
    public string DisplayName { get; private set; } = string.Empty;
    public string? Notes { get; private set; }

    private Customer() { }

    public static Customer Create(Guid tenantId, Guid businessId, string displayName, string? notes)
    {
        if (tenantId == Guid.Empty) throw new ArgumentException("Tenant is required.", nameof(tenantId));
        if (businessId == Guid.Empty) throw new ArgumentException("Business is required.", nameof(businessId));
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);

        return new Customer
        {
            TenantId = tenantId,
            BusinessId = businessId,
            DisplayName = displayName.Trim(),
            Notes = NullIfEmpty(notes)
        };
    }

    public void Update(string displayName, string? notes)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);
        DisplayName = displayName.Trim();
        Notes = NullIfEmpty(notes);
        Touch();
    }

    public CustomerContact AddContact(CustomerContactKind kind, string value) =>
        CustomerContact.Create(TenantId, BusinessId, Id, kind, value);

    private static string? NullIfEmpty(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

public sealed class CustomerContact : TenantOwnedEntity
{
    public Guid BusinessId { get; private set; }
    public Guid CustomerId { get; private set; }
    public CustomerContactKind Kind { get; private set; }
    public string Value { get; private set; } = string.Empty;

    private CustomerContact() { }

    public static CustomerContact Create(
        Guid tenantId,
        Guid businessId,
        Guid customerId,
        CustomerContactKind kind,
        string value)
    {
        if (tenantId == Guid.Empty) throw new ArgumentException("Tenant is required.", nameof(tenantId));
        if (businessId == Guid.Empty) throw new ArgumentException("Business is required.", nameof(businessId));
        if (customerId == Guid.Empty) throw new ArgumentException("Customer is required.", nameof(customerId));
        ArgumentException.ThrowIfNullOrWhiteSpace(value);

        var trimmed = value.Trim();
        if (kind == CustomerContactKind.Email && !trimmed.Contains('@', StringComparison.Ordinal))
        {
            throw new ArgumentException("Email contact must contain '@'.", nameof(value));
        }

        return new CustomerContact
        {
            TenantId = tenantId,
            BusinessId = businessId,
            CustomerId = customerId,
            Kind = kind,
            Value = trimmed
        };
    }

    public void Update(CustomerContactKind kind, string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        var trimmed = value.Trim();
        if (kind == CustomerContactKind.Email && !trimmed.Contains('@', StringComparison.Ordinal))
        {
            throw new ArgumentException("Email contact must contain '@'.", nameof(value));
        }

        Kind = kind;
        Value = trimmed;
        Touch();
    }
}
