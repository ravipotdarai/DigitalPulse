using DigitalPulse.Domain.Common;

namespace DigitalPulse.Domain.Billing;

public sealed class SubscriptionPlan : Entity
{
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public decimal MonthlyPriceInr { get; private set; }
    public decimal AnnualPriceInr { get; private set; }
    public int MaxBusinesses { get; private set; }
    public int MaxLocations { get; private set; } = 1;
    public int MaxConnections { get; private set; } = 5;
    public int ScansPerMonth { get; private set; } = 2;
    public int ActionsPerMonth { get; private set; } = 25;
    public int AiGenerationsPerMonth { get; private set; } = 50;
    public int MaxUsers { get; private set; } = 2;
    public int MaxAgencyClients { get; private set; }
    public int StorageGb { get; private set; } = 2;
    public bool WhiteLabel { get; private set; }
    public bool WhatsAppEnabled { get; private set; }
    public int WhatsAppMessagesPerMonth { get; private set; }
    public int MonitoringIntervalHours { get; private set; } = 168;
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
        int whatsAppMessagesPerMonth = 0,
        int monitoringIntervalHours = 168,
        int maxLocations = 1,
        int aiGenerationsPerMonth = 50,
        int maxUsers = 2,
        int maxAgencyClients = 0,
        int storageGb = 2,
        bool whiteLabel = false,
        decimal? annualPriceInr = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        if (monthlyPriceInr < 0) throw new ArgumentOutOfRangeException(nameof(monthlyPriceInr));
        if (maxBusinesses < 1) throw new ArgumentOutOfRangeException(nameof(maxBusinesses));
        if (maxConnections < 0) throw new ArgumentOutOfRangeException(nameof(maxConnections));
        if (scansPerMonth < 0) throw new ArgumentOutOfRangeException(nameof(scansPerMonth));
        if (actionsPerMonth < 0) throw new ArgumentOutOfRangeException(nameof(actionsPerMonth));
        if (whatsAppMessagesPerMonth < 0) throw new ArgumentOutOfRangeException(nameof(whatsAppMessagesPerMonth));
        if (monitoringIntervalHours < 1) throw new ArgumentOutOfRangeException(nameof(monitoringIntervalHours));
        if (maxLocations < 1) throw new ArgumentOutOfRangeException(nameof(maxLocations));
        if (aiGenerationsPerMonth < 0) throw new ArgumentOutOfRangeException(nameof(aiGenerationsPerMonth));
        if (maxUsers < 1) throw new ArgumentOutOfRangeException(nameof(maxUsers));
        if (maxAgencyClients < 0) throw new ArgumentOutOfRangeException(nameof(maxAgencyClients));
        if (storageGb < 0) throw new ArgumentOutOfRangeException(nameof(storageGb));
        var annual = annualPriceInr ?? monthlyPriceInr * 10;
        if (annual < 0) throw new ArgumentOutOfRangeException(nameof(annualPriceInr));

        return new SubscriptionPlan
        {
            Code = code.Trim().ToUpperInvariant(),
            Name = name.Trim(),
            MonthlyPriceInr = monthlyPriceInr,
            AnnualPriceInr = annual,
            MaxBusinesses = maxBusinesses,
            MaxLocations = maxLocations,
            MaxConnections = maxConnections,
            ScansPerMonth = scansPerMonth,
            ActionsPerMonth = actionsPerMonth,
            AiGenerationsPerMonth = aiGenerationsPerMonth,
            MaxUsers = maxUsers,
            MaxAgencyClients = maxAgencyClients,
            StorageGb = storageGb,
            WhiteLabel = whiteLabel,
            WhatsAppEnabled = whatsAppEnabled,
            WhatsAppMessagesPerMonth = whatsAppMessagesPerMonth,
            MonitoringIntervalHours = monitoringIntervalHours,
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
