using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using DigitalPulse.Application.Abstractions;
using Microsoft.Extensions.Configuration;

namespace DigitalPulse.Infrastructure.Billing;

public sealed class DevelopmentBillingGateway : IBillingGateway
{
    public string ProviderName => "Development";
    public bool IsLive => false;

    public Task<BillingCheckoutResult> CheckoutAsync(string planCode, decimal amountInr, string invoiceNumber, CancellationToken cancellationToken)
    {
        return Task.FromResult(new BillingCheckoutResult(
            "Held",
            ProviderName,
            invoiceNumber,
            $"Checkout for {planCode} ₹{amountInr:0} on {invoiceNumber} stays held. No payment was captured. Configure Billing:Razorpay keys for the official provider.",
            false));
    }

    public bool VerifyWebhook(string? signature, string payload) => false;
}

public sealed class RazorpayBillingGateway : IBillingGateway
{
    private readonly IHttpClientFactory _http;
    private readonly IConfiguration _configuration;
    private readonly string _webhookSecret;

    public RazorpayBillingGateway(IHttpClientFactory http, IConfiguration configuration)
    {
        _http = http;
        _configuration = configuration;
        _webhookSecret = configuration["Billing:Razorpay:WebhookSecret"] ?? string.Empty;
    }

    public string ProviderName => "Razorpay";
    public bool IsLive => true;

    public async Task<BillingCheckoutResult> CheckoutAsync(string planCode, decimal amountInr, string invoiceNumber, CancellationToken cancellationToken)
    {
        var keyId = _configuration["Billing:Razorpay:KeyId"];
        var secret = _configuration["Billing:Razorpay:KeySecret"];
        if (string.IsNullOrWhiteSpace(keyId) || string.IsNullOrWhiteSpace(secret))
        {
            return new BillingCheckoutResult(
                "Held",
                ProviderName,
                invoiceNumber,
                "Razorpay keys are missing. No payment was captured.",
                false);
        }

        var paise = (long)decimal.Round(amountInr * 100m, 0, MidpointRounding.AwayFromZero);
        using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.razorpay.com/v1/orders")
        {
            Content = new StringContent(
                JsonSerializer.Serialize(new
                {
                    amount = paise,
                    currency = "INR",
                    receipt = invoiceNumber,
                    payment_capture = 0
                }),
                Encoding.UTF8,
                "application/json")
        };
        request.Headers.Authorization = new AuthenticationHeaderValue(
            "Basic",
            Convert.ToBase64String(Encoding.UTF8.GetBytes($"{keyId}:{secret}")));

        using var response = await _http.CreateClient("official-platforms").SendAsync(request, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return new BillingCheckoutResult(
                "Held",
                ProviderName,
                invoiceNumber,
                $"Razorpay order returned {(int)response.StatusCode}. Invoice stays unpaid. DigitalPulse did not invent a capture.",
                true);
        }

        using var doc = JsonDocument.Parse(string.IsNullOrWhiteSpace(body) ? "{}" : body);
        var orderId = doc.RootElement.TryGetProperty("id", out var id) ? id.GetString() : null;
        if (string.IsNullOrWhiteSpace(orderId))
        {
            return new BillingCheckoutResult(
                "Held",
                ProviderName,
                invoiceNumber,
                "Razorpay did not return an order id. Invoice stays unpaid.",
                true);
        }

        return new BillingCheckoutResult(
            "OrderCreated",
            ProviderName,
            orderId,
            $"Razorpay order {orderId} created for {planCode} ₹{amountInr:0}. Complete checkout on Razorpay. Invoice stays unpaid until a signed webhook confirms capture.",
            true);
    }

    public bool VerifyWebhook(string? signature, string payload)
    {
        if (string.IsNullOrWhiteSpace(_webhookSecret) || string.IsNullOrWhiteSpace(signature) || string.IsNullOrWhiteSpace(payload))
        {
            return false;
        }

        var expected = Convert.ToHexString(HMACSHA256.HashData(Encoding.UTF8.GetBytes(_webhookSecret), Encoding.UTF8.GetBytes(payload)));
        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(expected),
            Encoding.UTF8.GetBytes(signature.Trim()));
    }
}
