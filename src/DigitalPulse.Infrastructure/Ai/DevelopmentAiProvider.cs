using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
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
    private readonly string _model;

    public OpenAiProvider(HttpClient http, IConfiguration configuration)
    {
        _http = http;
        ProviderName = "OpenAI";
        _model = configuration["Ai:OpenAi:Model"] ?? "gpt-4o-mini";
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
        using var payload = new StringContent(JsonSerializer.Serialize(new
        {
            model = _model,
            temperature = 0.2,
            messages = new object[]
            {
                new
                {
                    role = "system",
                    content = "You are a DigitalPulse agent. Use only the supplied evidence. Never invent facts. Never publish restricted facts. If evidence conflicts, say so and ask for review."
                },
                new { role = "user", content = request.Prompt }
            }
        }), Encoding.UTF8, "application/json");

        using var response = await _http.PostAsync("https://api.openai.com/v1/chat/completions", payload, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return new AiCompletionResponse(
                $"OpenAI returned {(int)response.StatusCode}. The run stays held. DigitalPulse did not invent a completion.",
                ProviderName,
                false);
        }

        using var doc = JsonDocument.Parse(body);
        var text = doc.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString();
        return new AiCompletionResponse(
            string.IsNullOrWhiteSpace(text) ? "The provider returned an empty completion." : text.Trim(),
            ProviderName,
            true);
    }
}
