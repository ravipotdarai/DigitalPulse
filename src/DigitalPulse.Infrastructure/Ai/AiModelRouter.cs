using DigitalPulse.Application.Abstractions;
using Microsoft.Extensions.Configuration;

namespace DigitalPulse.Infrastructure.Ai;

public sealed class AiModelRouter : IAiModelRouter
{
    private readonly IConfiguration _configuration;

    public AiModelRouter(IConfiguration configuration) => _configuration = configuration;

    public string Select(string agentCode)
    {
        var named = _configuration[$"Ai:Models:{agentCode}"];
        if (!string.IsNullOrWhiteSpace(named)) return named.Trim();

        var low = agentCode is "research" or "automation" or "seo";
        var key = low ? "Ai:Models:Low" : "Ai:Models:Standard";
        return First(
            _configuration[key],
            _configuration["Ai:DefaultModel"],
            _configuration["Ai:OpenAi:Model"],
            _configuration["Ai:AzureOpenAi:Deployment"],
            "gpt-4o-mini");
    }

    private static string First(params string?[] values) =>
        values.First(value => !string.IsNullOrWhiteSpace(value))!.Trim();
}
