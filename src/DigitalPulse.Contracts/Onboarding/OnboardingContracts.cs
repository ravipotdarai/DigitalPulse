namespace DigitalPulse.Contracts.Onboarding;

public sealed record OnboardingStatusResponse(
    bool HasTenant,
    bool HasBusiness,
    bool HasLocation,
    bool HasSubscription,
    string NextStep);

public sealed record DashboardResponse(
    Guid TenantId,
    string TenantName,
    string TenantType,
    string PlanName,
    int MaxBusinesses,
    int BusinessCount,
    int LocationCount,
    IReadOnlyList<DashboardBusinessResponse> Businesses);

public sealed record DashboardBusinessResponse(
    Guid Id,
    string Name,
    string? Website,
    int LocationCount);
