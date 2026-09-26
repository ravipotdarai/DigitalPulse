using DigitalPulse.Application.Ai;
using DigitalPulse.Domain.Ai;
using Xunit;

namespace DigitalPulse.UnitTests;

public sealed class AiAgentCatalogPhase5Tests
{
    [Fact]
    public void Required_agents_have_versioned_prompts_and_cannot_execute()
    {
        foreach (var code in AiAgents.Required)
        {
            var run = AiAgents.Require(code);
            var agent = AiAgentCatalog.Require(code);
            var prompt = AiPromptCatalog.Build(agent, "Ask: outline", [new AiEvidence("fact", "NAME", "AV Professionals", false, true)], ["Business: AV Professionals"], false);
            Assert.Equal($"{code}.v1", run.PromptVersion);
            Assert.False(run.MayExecuteExternally);
            Assert.Contains($"Prompt-Version: {code}.v1", prompt);
        }
    }

    [Fact]
    public void Unknown_agent_is_rejected()
    {
        Assert.Throws<ArgumentException>(() => AiAgents.Require("vendor-sdk"));
    }
}
