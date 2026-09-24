namespace DigitalPulse.Application.Abstractions;

public sealed record BillingCheckoutResult(string Status, string Provider, string? Reference, string Detail, bool IsLive);

public sealed record BillingWebhookIngest(string? Signature, string Payload, string? EventType);

public sealed record BillingWebhookResult(Guid EventId, bool SignatureValid, bool Processed, string HoldReason);

public interface IBillingGateway
{
    string ProviderName { get; }
    bool IsLive { get; }
    Task<BillingCheckoutResult> CheckoutAsync(string planCode, decimal amountInr, string invoiceNumber, CancellationToken cancellationToken);
    bool VerifyWebhook(string? signature, string payload);
}
