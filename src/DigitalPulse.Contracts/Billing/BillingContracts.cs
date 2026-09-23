namespace DigitalPulse.Contracts.Billing;

public sealed record PlanResponse(
    Guid Id,
    string Code,
    string Name,
    decimal MonthlyPriceInr,
    int MaxBusinesses,
    bool AgencyOnly);

public sealed record SelectPlanRequest(string PlanCode);

public sealed record SubscriptionResponse(
    Guid Id,
    Guid TenantId,
    string PlanCode,
    string PlanName,
    decimal MonthlyPriceInr,
    int MaxBusinesses,
    string Status);
