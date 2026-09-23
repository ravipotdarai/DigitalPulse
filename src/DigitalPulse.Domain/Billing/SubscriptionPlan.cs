using DigitalPulse.Domain.Common;

namespace DigitalPulse.Domain.Billing;

public sealed class SubscriptionPlan : Entity
{
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public decimal MonthlyPriceInr { get; private set; }
    public int MaxBusinesses { get; private set; }
    public bool AgencyOnly { get; private set; }
    public int SortOrder { get; private set; }

    private SubscriptionPlan() { }

    public static SubscriptionPlan Create(
        string code,
        string name,
        decimal monthlyPriceInr,
        int maxBusinesses,
        bool agencyOnly,
        int sortOrder)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        if (monthlyPriceInr < 0) throw new ArgumentOutOfRangeException(nameof(monthlyPriceInr));
        if (maxBusinesses < 1) throw new ArgumentOutOfRangeException(nameof(maxBusinesses));

        return new SubscriptionPlan
        {
            Code = code.Trim().ToUpperInvariant(),
            Name = name.Trim(),
            MonthlyPriceInr = monthlyPriceInr,
            MaxBusinesses = maxBusinesses,
            AgencyOnly = agencyOnly,
            SortOrder = sortOrder
        };
    }

    public bool IsAvailableTo(Tenancy.TenantType tenantType) => tenantType switch
    {
        Tenancy.TenantType.Agency => AgencyOnly,
        Tenancy.TenantType.Direct => !AgencyOnly,
        _ => false
    };
}
