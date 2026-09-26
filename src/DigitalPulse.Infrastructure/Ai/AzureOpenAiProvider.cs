using System.Text;
using System.Text.Json;
using DigitalPulse.Application.Abstractions;
using Microsoft.Extensions.Configuration;

namespace DigitalPulse.Infrastructure.Ai;

public sealed class AzureOpenAiProvider : IAiProvider
{
    private readonly HttpClient _http;
    private readonly IConfiguration _configuration;

    public AzureOpenAiProvider(HttpClient http, IConfiguration configuration)
    {
        _http = http;
        _configuration = configuration;
        ProviderName = "AzureOpenAI";
        var key = configuration["Ai:AzureOpenAi:ApiKey"];
        if (!string.IsNullOrWhiteSpace(key))
        {
            _http.DefaultRequestHeaders.Remove("api-key");
            _http.DefaultRequestHeaders.TryAddWithoutValidation("api-key", key);
        }
    }

    public string ProviderName { get; }
    public bool IsLive => true;

    public async Task<AiCompletionResponse> CompleteAsync(AiCompletionRequest request, CancellationToken cancellationToken)
    {
        var endpoint = (_configuration["Ai:AzureOpenAi:Endpoint"] ?? string.Empty).Trim().TrimEnd('/');
        var deployment = request.Model
            ?? _configuration["Ai:AzureOpenAi:Deployment"]
            ?? _configuration["Ai:DefaultModel"]
            ?? "gpt-4o-mini";
        var version = _configuration["Ai:AzureOpenAi:ApiVersion"] ?? "2024-10-21";
        if (string.IsNullOrWhiteSpace(endpoint))
        {
            return new AiCompletionResponse(
                "Azure OpenAI endpoint is not configured. The run stays held. DigitalPulse did not invent a completion.",
                ProviderName,
                false,
                deployment);
        }

        var url = $"{endpoint}/openai/deployments/{Uri.EscapeDataString(deployment)}/chat/completions?api-version={Uri.EscapeDataString(version)}";
        using var payload = ChatCompletions.Request(_configuration, request, deployment);
        using var response = await _http.PostAsync(url, payload, cancellationToken);
        return await ChatCompletions.ReadAsync(response, ProviderName, deployment, cancellationToken);
    }
}

internal static class ChatCompletions
{
    public static StringContent Request(IConfiguration configuration, AiCompletionRequest request, string model)
    {
        var temperature = double.TryParse(configuration["Ai:Temperature"], out var parsed) ? parsed : 0.2;
        var maxTokens = request.MaxOutputTokens
            ?? (int.TryParse(configuration["Ai:MaxOutputTokens"], out var tokens) ? tokens : 4000);
        return new StringContent(JsonSerializer.Serialize(new
        {
            model,
            temperature,
            max_tokens = maxTokens,
            messages = new object[]
            {
                new
                {
                    role = "system",
                    content = "You are a DigitalPulse agent. Use only the supplied evidence. Never invent facts. Never publish restricted facts. If evidence conflicts, say so and ask for review. Return ordinary text unless the prompt asks for a JSON object."
                },
                new { role = "user", content = request.Prompt }
            }
        }), Encoding.UTF8, "application/json");
    }

    public static async Task<AiCompletionResponse> ReadAsync(
        HttpResponseMessage response,
        string providerName,
        string model,
        CancellationToken cancellationToken)
    {
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return new AiCompletionResponse(
                $"{providerName} returned {(int)response.StatusCode}. The run stays held. DigitalPulse did not invent a completion.",
                providerName,
                false,
                model);
        }

        using var doc = JsonDocument.Parse(body);
        var root = doc.RootElement;
        var text = root.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString();
        int? promptTokens = null;
        int? completionTokens = null;
        if (root.TryGetProperty("usage", out var usage))
        {
            if (usage.TryGetProperty("prompt_tokens", out var prompt)) promptTokens = prompt.GetInt32();
            if (usage.TryGetProperty("completion_tokens", out var completion)) completionTokens = completion.GetInt32();
        }

        return new AiCompletionResponse(
            string.IsNullOrWhiteSpace(text) ? "The provider returned an empty completion." : text.Trim(),
            providerName,
            true,
            model,
            promptTokens,
            completionTokens);
    }
}
