using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using DigitalPulse.Application.Abstractions;
using Microsoft.Extensions.Configuration;

namespace DigitalPulse.Infrastructure.WhatsApp;

public sealed class DevelopmentWhatsAppCloudApi : IWhatsAppCloudApi
{
    public string ProviderName => "Development";
    public bool IsLive => false;

    public Task<WhatsAppCloudResult> HealthAsync(CancellationToken cancellationToken) =>
        Task.FromResult(Hold("Cloud API health is not available. A development grant is not a live WhatsApp Business account."));

    public Task<WhatsAppCloudResult> VerifyPhoneAsync(string phoneNumber, CancellationToken cancellationToken)
    {
        _ = phoneNumber;
        return Task.FromResult(Hold("Business phone verification waits for WhatsApp Cloud API. DigitalPulse will not invent a verified number."));
    }

    public Task<WhatsAppCloudResult> SendTemplateAsync(string to, string templateName, string body, CancellationToken cancellationToken)
    {
        _ = to;
        _ = templateName;
        _ = body;
        return Task.FromResult(Hold("Template send stays held. Official Cloud API is not configured. Unofficial WhatsApp automation is out of scope."));
    }

    public Task<WhatsAppCloudResult> SendSessionAsync(string to, string body, CancellationToken cancellationToken)
    {
        _ = to;
        _ = body;
        return Task.FromResult(Hold("Session send stays held. Official Cloud API is not configured."));
    }

    private static WhatsAppCloudResult Hold(string detail) => new("Hold", detail, null, false);
}

public sealed class LiveWhatsAppCloudApi : IWhatsAppCloudApi
{
    private readonly HttpClient _http;
    private readonly string? _phoneNumberId;

    public LiveWhatsAppCloudApi(HttpClient http, IConfiguration configuration)
    {
        _http = http;
        ProviderName = "WhatsApp Cloud API";
        _phoneNumberId = configuration["WhatsApp:CloudApi:PhoneNumberId"];
        var token = configuration["WhatsApp:CloudApi:AccessToken"];
        if (!string.IsNullOrWhiteSpace(token))
        {
            _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }
    }

    public string ProviderName { get; }
    public bool IsLive => !string.IsNullOrWhiteSpace(_phoneNumberId);

    public Task<WhatsAppCloudResult> HealthAsync(CancellationToken cancellationToken) =>
        CallAsync(HttpMethod.Get, $"{_phoneNumberId}", null, cancellationToken);

    public Task<WhatsAppCloudResult> VerifyPhoneAsync(string phoneNumber, CancellationToken cancellationToken) =>
        CallAsync(HttpMethod.Get, $"{_phoneNumberId}", null, cancellationToken, phoneNumber);

    public Task<WhatsAppCloudResult> SendTemplateAsync(string to, string templateName, string body, CancellationToken cancellationToken)
    {
        _ = body;
        var payload = JsonSerializer.Serialize(new
        {
            messaging_product = "whatsapp",
            to,
            type = "template",
            template = new { name = templateName, language = new { code = "en" } }
        });
        return CallAsync(HttpMethod.Post, $"{_phoneNumberId}/messages", payload, cancellationToken);
    }

    public Task<WhatsAppCloudResult> SendSessionAsync(string to, string body, CancellationToken cancellationToken)
    {
        var payload = JsonSerializer.Serialize(new
        {
            messaging_product = "whatsapp",
            to,
            type = "text",
            text = new { body }
        });
        return CallAsync(HttpMethod.Post, $"{_phoneNumberId}/messages", payload, cancellationToken);
    }

    private async Task<WhatsAppCloudResult> CallAsync(
        HttpMethod method,
        string path,
        string? json,
        CancellationToken cancellationToken,
        string? phoneHint = null)
    {
        if (string.IsNullOrWhiteSpace(_phoneNumberId))
        {
            return new("Hold", "Cloud API token is present but PhoneNumberId is missing. A send was not invented.", null, false);
        }

        using var request = new HttpRequestMessage(method, path);
        if (json is not null)
        {
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");
        }

        using var response = await _http.SendAsync(request, cancellationToken);
        var raw = await response.Content.ReadAsStringAsync(cancellationToken);
        if (response.IsSuccessStatusCode)
        {
            using var doc = JsonDocument.Parse(string.IsNullOrWhiteSpace(raw) ? "{}" : raw);
            var id = doc.RootElement.TryGetProperty("messages", out var messages) && messages.GetArrayLength() > 0
                ? messages[0].GetProperty("id").GetString()
                : phoneHint;
            return new("Accepted", "Official Cloud API accepted the request.", id, true);
        }

        return new("Failed", $"Cloud API returned {(int)response.StatusCode}. DigitalPulse did not invent a delivery.", null, true);
    }
}
