using DigitalPulse.Domain.Ai;

namespace DigitalPulse.Application.Ai;

public sealed record AiAgentRun(string Code, string PromptVersion, bool MayExecuteExternally);

public static class AiAgents
{
    public static IReadOnlyList<string> Required =>
        ["research", "identity", "seo", "content", "social", "reputation", "portfolio", "competitor", "automation", "whatsapp"];

    public static AiAgentRun Require(string code)
    {
        var agent = AiAgentCatalog.Require(code);
        return new AiAgentRun(agent.Code, $"{agent.Code}.{AiPromptCatalog.Version}", MayExecuteExternally: false);
    }
}
