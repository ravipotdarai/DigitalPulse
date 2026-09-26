using System.Text.RegularExpressions;
using DigitalPulse.Application.Common;
using DigitalPulse.Domain.Ai;
using DigitalPulse.Domain.Safety;

namespace DigitalPulse.Application.Ai;

public static class AiGuardrails
{
    private static readonly Regex Injection = new(
        @"ignore ((all|any|the) )?(previous|prior|above) instructions|you are now|system prompt|reveal (your|the) (system )?prompt|jailbreak|developer mode|do not follow digitalpulse",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

    public static void RequireInput(string ask)
    {
        ContentGuard.Require(ask);
        if (LooksLikeInjection(ask))
        {
            throw AppException.Validation("That ask looks like a prompt-injection attempt. DigitalPulse will not override policy from user text.");
        }
    }

    public static bool LooksLikeInjection(string? text) =>
        !string.IsNullOrWhiteSpace(text) && Injection.IsMatch(text);

    public static AiValidationResult AfterModel(AiValidationResult current, string output, IReadOnlyList<AiEvidence> evidence)
    {
        if (LooksLikeInjection(output))
        {
            return new AiValidationResult(
                false,
                current.HasEvidence,
                current.HasConflict,
                current.HasRestrictedFact,
                AiConfidence.Low,
                AiRunStatus.NeedsReview,
                "The model returned instruction-like text. It stays held.");
        }

        if (!ContentSafety.Assess(output).Allowed)
        {
            return new AiValidationResult(
                false,
                current.HasEvidence,
                current.HasConflict,
                current.HasRestrictedFact,
                AiConfidence.Low,
                AiRunStatus.Rejected,
                ContentSafety.BanMessage);
        }

        var ungrounded = evidence.Count > 0
            && evidence.All(item => item.Restricted || string.IsNullOrWhiteSpace(item.Body))
            && output.Contains("http://", StringComparison.OrdinalIgnoreCase);
        if (ungrounded)
        {
            return new AiValidationResult(
                false,
                current.HasEvidence,
                current.HasConflict,
                current.HasRestrictedFact,
                AiConfidence.Low,
                AiRunStatus.NeedsReview,
                "Output introduced a URL without publishable evidence.");
        }

        return current;
    }
}
