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
    IReadOnlyList<DashboardBusinessResponse> Businesses,
    DateTimeOffset? LastScanAtUtc,
    int OpenFindingCount,
    int HighFindingCount,
    IReadOnlyList<DashboardFindingResponse> TopFindings,
    DateTimeOffset? LastWebsiteAtUtc,
    int WebsiteObservationCount,
    string SearchProvider,
    int SocialDraftCount,
    int SocialBlockedCount,
    int DirectoryOpenCount,
    int DirectoryVerifiedCount,
    int ProjectCount,
    int ContentHoldCount,
    int AiRunCount,
    int AiHeldCount,
    int ActionOpenCount,
    int ActionHeldCount,
    int WhatsAppOptInCount,
    int WhatsAppHeldCount,
    DateTimeOffset? LastMonitoringAtUtc,
    int OpenAlertCount,
    int ReportCount,
    int MonitoringIntervalHours,
    string MonitoringHoldReason,
    string SubscriptionStatus,
    string BillingHoldReason,
    int HeldInvoiceCount,
    int AgencyClientCount,
    bool WhiteLabelEnabled,
    string AgencyHoldReason);

public sealed record DashboardBusinessResponse(
    Guid Id,
    string Name,
    string? Website,
    int LocationCount);

public sealed record DashboardFindingResponse(
    Guid Id,
    Guid BusinessId,
    string Severity,
    string Category,
    string Title,
    string Status);
