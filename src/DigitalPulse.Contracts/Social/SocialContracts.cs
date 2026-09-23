namespace DigitalPulse.Contracts.Social;

public sealed record SocialChannelResponse(
    string PlatformCode,
    string PlatformName,
    string Category,
    bool CanPublish,
    bool CanGetMetrics,
    bool AssistedOnly,
    string? ConnectionStatus,
    string? GrantKind,
    string? MetricStatus,
    string? MetricDetail);

public sealed record SocialContentResponse(
    Guid Id,
    Guid BusinessId,
    string PlatformCode,
    string Kind,
    string Title,
    string Body,
    string Status,
    string VerificationStatus,
    string? VerificationDetail,
    string? LastPublishError,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);

public sealed record SocialWorkspaceResponse(
    IReadOnlyList<SocialChannelResponse> Channels,
    IReadOnlyList<SocialContentResponse> Items,
    string Note);

public sealed record CreateSocialContentRequest(string PlatformCode, string Title, string Body);
public sealed record UpdateSocialContentRequest(string Title, string Body);
