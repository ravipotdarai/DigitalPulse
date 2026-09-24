namespace DigitalPulse.Application.Abstractions;

public sealed record WhatsAppCloudResult(string Status, string Detail, string? ProviderMessageId, bool IsLive);

public interface IWhatsAppCloudApi
{
    string ProviderName { get; }
    bool IsLive { get; }
    Task<WhatsAppCloudResult> HealthAsync(CancellationToken cancellationToken);
    Task<WhatsAppCloudResult> VerifyPhoneAsync(string phoneNumber, CancellationToken cancellationToken);
    Task<WhatsAppCloudResult> SendTemplateAsync(string to, string templateName, string body, CancellationToken cancellationToken);
    Task<WhatsAppCloudResult> SendSessionAsync(string to, string body, CancellationToken cancellationToken);
}
