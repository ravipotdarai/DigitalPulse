namespace DigitalPulse.Contracts.Billing;

public sealed record PlanResponse(
    Guid Id,
    string Code,
    string Name,
    decimal MonthlyPriceInr,
    decimal AnnualPriceInr,
    int MaxBusinesses,
    int MaxLocations,
    int MaxConnections,
    int ScansPerMonth,
    int ActionsPerMonth,
    int AiGenerationsPerMonth,
    int MaxUsers,
    int MaxAgencyClients,
    int StorageGb,
    bool WhiteLabel,
    bool WhatsAppEnabled,
    int WhatsAppMessagesPerMonth,
    int MonitoringIntervalHours,
    bool AgencyOnly);

public sealed record SelectPlanRequest(string PlanCode, string? Interval = null);

public sealed record ChangePlanRequest(string PlanCode, string? Interval = null);

public sealed record CheckoutRequest(Guid? InvoiceId = null);

public sealed record CancelSubscriptionRequest(bool Immediately, string? Reason = null);

public sealed record SubscriptionResponse(
    Guid Id,
    Guid TenantId,
    string PlanCode,
    string PlanName,
    decimal MonthlyPriceInr,
    int MaxBusinesses,
    string Status,
    string Interval,
    DateTimeOffset PeriodStartUtc,
    DateTimeOffset PeriodEndUtc,
    bool CancelAtPeriodEnd,
    string HoldReason);

public sealed record UsageMeterResponse(string Kind, int Used, int Included, string Note);

public sealed record InvoiceResponse(
    Guid Id,
    string Number,
    string Status,
    string Interval,
    decimal AmountInr,
    DateTimeOffset PeriodStartUtc,
    DateTimeOffset PeriodEndUtc,
    DateTimeOffset IssuedAtUtc,
    DateTimeOffset? PaidAtUtc,
    string HoldReason,
    IReadOnlyList<string> Lines);

public sealed record PaymentAttemptResponse(Guid Id, string Status, string Provider, string? ProviderReference, string Detail, DateTimeOffset CreatedAtUtc);

public sealed record BillingWebhookResponse(Guid Id, string Provider, string EventType, bool SignatureValid, bool Untrusted, bool Processed, string HoldReason);

public sealed record BillingWorkspaceResponse(
    SubscriptionResponse? Subscription,
    PlanResponse? Plan,
    bool ProviderIsLive,
    string ProviderName,
    string Note,
    IReadOnlyList<PlanResponse> AvailablePlans,
    IReadOnlyList<UsageMeterResponse> Usage,
    IReadOnlyList<InvoiceResponse> Invoices,
    IReadOnlyList<PaymentAttemptResponse> Payments,
    IReadOnlyList<BillingWebhookResponse> Webhooks);
