using System.Security.Cryptography;
using System.Text;
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
    private readonly string _webhookSecret;

    public RazorpayBillingGateway(IConfiguration configuration)
    {
        _webhookSecret = configuration["Billing:Razorpay:WebhookSecret"] ?? string.Empty;
    }

    public string ProviderName => "Razorpay";
    public bool IsLive => true;

    public Task<BillingCheckoutResult> CheckoutAsync(string planCode, decimal amountInr, string invoiceNumber, CancellationToken cancellationToken)
    {
        return Task.FromResult(new BillingCheckoutResult(
            "Held",
            ProviderName,
            invoiceNumber,
            $"Razorpay is configured for {planCode} ₹{amountInr:0}. DigitalPulse did not invent a captured payment. Complete checkout with the official Razorpay order and a signed webhook.",
            true));
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
