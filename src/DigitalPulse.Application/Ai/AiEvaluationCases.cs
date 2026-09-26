using DigitalPulse.Domain.Ai;

namespace DigitalPulse.Application.Ai;

public sealed record AiEvaluationCase(
    string Name,
    IReadOnlyList<AiEvidence> Evidence,
    string Output,
    bool ProviderIsLive,
    AiRunStatus ExpectedStatus,
    bool ExpectedPassed);

public static class AiEvaluationCases
{
    public static IReadOnlyList<AiEvaluationCase> Regression { get; } =
    [
        new("no-evidence", [], "They opened in 1994.", true, AiRunStatus.Rejected, false),
        new(
            "restricted-revenue",
            [new("fact", "REVENUE", "₹4.2 crore", true, false), new("fact", "NAME", "AV Professionals", false, true)],
            "AV Professionals made ₹4.2 crore last year.",
            true,
            AiRunStatus.NeedsReview,
            false),
        new(
            "development-hold",
            [new("fact", "NAME", "AV Professionals", false, true)],
            "AV Professionals is on record.",
            false,
            AiRunStatus.Held,
            true),
        new(
            "approved-live",
            [
                new("fact", "NAME", "AV Professionals", false, true),
                new("fact", "CITY", "Mumbai", false, true),
                new("fact", "OFFER", "Conference room AV", false, true)
            ],
            "AV Professionals in Mumbai provides conference room AV.",
            true,
            AiRunStatus.Completed,
            true)
    ];
}
