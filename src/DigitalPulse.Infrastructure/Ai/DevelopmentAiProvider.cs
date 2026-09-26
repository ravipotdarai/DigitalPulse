using System.Net.Http.Headers;
using DigitalPulse.Application.Abstractions;
using Microsoft.Extensions.Configuration;

namespace DigitalPulse.Infrastructure.Ai;

public sealed class DevelopmentAiProvider : IAiProvider
{
    public string ProviderName => "Development";
    public bool IsLive => false;

    public Task<AiCompletionResponse> CompleteAsync(AiCompletionRequest request, CancellationToken cancellationToken)
    {
        var lines = new List<string>
        {
            $"Development composition for the {request.AgentCode} agent.",
            "A live AI provider is not configured. This text is assembled from retrieved evidence only.",
            "",
            "Ask:",
            request.Prompt.Split(Environment.NewLine).FirstOrDefault(l => l.StartsWith("Ask:", StringComparison.Ordinal)) ?? request.Prompt,
            "",
            "Evidence used:"
        };

        if (request.Evidence.Count == 0)
        {
            lines.Add("- None. DigitalPulse will not invent a factual claim.");
        }
        else
        {
            foreach (var item in request.Evidence.Where(e => !e.Restricted).Take(12))
            {
                lines.Add($"- {item.Title}: {Trim(item.Body, 220)}");
            }

            if (request.Evidence.Any(e => e.Restricted))
            {
                lines.Add("- Restricted facts were retrieved and withheld from this composition.");
            }
        }

        if (request.GraphContext.Count > 0)
        {
            lines.Add("");
            lines.Add("Graphify context:");
            foreach (var node in request.GraphContext.Take(12))
            {
                lines.Add($"- {node}");
            }
        }

        lines.Add("");
        lines.Add("This stay is held for review. Autopilot and live execution wait for later phases.");
        return Task.FromResult(new AiCompletionResponse(string.Join(Environment.NewLine, lines), ProviderName, false));
    }

    private static string Trim(string value, int max) =>
        value.Length <= max ? value : value[..max] + "…";
}

public sealed class OpenAiProvider : IAiProvider
{
    private readonly HttpClient _http;
    private readonly IConfiguration _configuration;
    private readonly string _model;

    public OpenAiProvider(HttpClient http, IConfiguration configuration)
    {
        _http = http;
        _configuration = configuration;
        ProviderName = "OpenAI";
        _model = configuration["Ai:OpenAi:Model"] ?? configuration["Ai:DefaultModel"] ?? "gpt-4o-mini";
        var key = configuration["Ai:OpenAi:ApiKey"];
        if (!string.IsNullOrWhiteSpace(key))
        {
            _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", key);
        }
    }

    public string ProviderName { get; }
    public bool IsLive => true;

    public async Task<AiCompletionResponse> CompleteAsync(AiCompletionRequest request, CancellationToken cancellationToken)
    {
        var model = request.Model ?? _model;
        using var payload = ChatCompletions.Request(_configuration, request, model);
        using var response = await _http.PostAsync("https://api.openai.com/v1/chat/completions", payload, cancellationToken);
        return await ChatCompletions.ReadAsync(response, ProviderName, model, cancellationToken);
    }
}
