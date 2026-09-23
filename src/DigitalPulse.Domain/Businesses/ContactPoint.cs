using DigitalPulse.Domain.Common;

namespace DigitalPulse.Domain.Businesses;

public enum ContactPointKind
{
    Phone = 1,
    Email = 2,
    Website = 3,
    SocialUrl = 4,
    DirectoryUrl = 5
}

public sealed class ContactPoint : TenantOwnedEntity
{
    public Guid BusinessId { get; private set; }
    public ContactPointKind Kind { get; private set; }
    public string Value { get; private set; } = string.Empty;
    public string? Label { get; private set; }

    private ContactPoint() { }

    public static ContactPoint Create(Guid tenantId, Guid businessId, ContactPointKind kind, string value, string? label)
    {
        RequireIds(tenantId, businessId);
        return new ContactPoint
        {
            TenantId = tenantId,
            BusinessId = businessId,
            Kind = kind,
            Value = Normalize(kind, value),
            Label = NullIfEmpty(label)
        };
    }

    public void Update(ContactPointKind kind, string value, string? label)
    {
        Kind = kind;
        Value = Normalize(kind, value);
        Label = NullIfEmpty(label);
        Touch();
    }

    private static void RequireIds(Guid tenantId, Guid businessId)
    {
        if (tenantId == Guid.Empty) throw new ArgumentException("Tenant is required.", nameof(tenantId));
        if (businessId == Guid.Empty) throw new ArgumentException("Business is required.", nameof(businessId));
    }

    private static string Normalize(ContactPointKind kind, string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        var trimmed = value.Trim();
        if (kind == ContactPointKind.Email && !trimmed.Contains('@', StringComparison.Ordinal))
        {
            throw new ArgumentException("Email contact must contain '@'.", nameof(value));
        }

        return trimmed;
    }

    private static string? NullIfEmpty(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
