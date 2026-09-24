using DigitalPulse.Domain.Common;

namespace DigitalPulse.Domain.Billing;

public sealed class SubscriptionPlan : Entity
{
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public decimal MonthlyPriceInr { get; private set; }
    public int MaxBusinesses { get; private set; }
    public int MaxConnections { get; private set; } = 5;
    public int ScansPerMonth { get; private set; } = 2;
    public int ActionsPerMonth { get; private set; } = 25;
    public bool WhatsAppEnabled { get; private set; }
    public int WhatsAppMessagesPerMonth { get; private set; }
    public bool AgencyOnly { get; private set; }
    public int SortOrder { get; private set; }

    private SubscriptionPlan() { }

    public static SubscriptionPlan Create(
        string code,
        string name,
        decimal monthlyPriceInr,
        int maxBusinesses,
        bool agencyOnly,
        int sortOrder,
        int maxConnections = 5,
        int scansPerMonth = 2,
        int actionsPerMonth = 25,
        bool whatsAppEnabled = false,
        int whatsAppMessagesPerMonth = 0)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        if (monthlyPriceInr < 0) throw new ArgumentOutOfRangeException(nameof(monthlyPriceInr));
        if (maxBusinesses < 1) throw new ArgumentOutOfRangeException(nameof(maxBusinesses));
        if (maxConnections < 0) throw new ArgumentOutOfRangeException(nameof(maxConnections));
        if (scansPerMonth < 0) throw new ArgumentOutOfRangeException(nameof(scansPerMonth));
        if (actionsPerMonth < 0) throw new ArgumentOutOfRangeException(nameof(actionsPerMonth));
        if (whatsAppMessagesPerMonth < 0) throw new ArgumentOutOfRangeException(nameof(whatsAppMessagesPerMonth));

        return new SubscriptionPlan
        {
            Code = code.Trim().ToUpperInvariant(),
            Name = name.Trim(),
            MonthlyPriceInr = monthlyPriceInr,
            MaxBusinesses = maxBusinesses,
            MaxConnections = maxConnections,
            ScansPerMonth = scansPerMonth,
            ActionsPerMonth = actionsPerMonth,
            WhatsAppEnabled = whatsAppEnabled,
            WhatsAppMessagesPerMonth = whatsAppMessagesPerMonth,
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
