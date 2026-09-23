using DigitalPulse.Application.Abstractions;
using DigitalPulse.Application.Common;
using DigitalPulse.Contracts.Businesses;
using DigitalPulse.Domain.Businesses;
using Microsoft.EntityFrameworkCore;

namespace DigitalPulse.Application.Features.Identity;

internal static class BusinessAccess
{
    public static BusinessResponse ToResponse(this Business business) =>
        new(business.Id, business.TenantId, business.Name, business.Website,
            business.FoundedYear, business.BrandVoice, business.IndustryCode);

    public static async Task<Business> RequireAsync(
        IAppDbContext db,
        Guid tenantId,
        Guid businessId,
        CancellationToken cancellationToken)
    {
        return await db.Businesses.FirstOrDefaultAsync(b => b.Id == businessId && b.TenantId == tenantId, cancellationToken)
            ?? throw AppException.NotFound("Business was not found.");
    }

    public static ContactPointKind ParseContactKind(string kind) =>
        Enum.TryParse<ContactPointKind>(kind, true, out var parsed)
            ? parsed
            : throw AppException.Validation("Contact kind must be Phone, Email, Website, SocialUrl, or DirectoryUrl.");

    public static FactStatus ParseFactStatus(string status) =>
        Enum.TryParse<FactStatus>(status, true, out var parsed)
            ? parsed
            : throw AppException.Validation("Fact status must be Draft, Approved, or Restricted.");

    public static CustomerContactKind ParseCustomerContactKind(string kind) =>
        Enum.TryParse<CustomerContactKind>(kind, true, out var parsed)
            ? parsed
            : throw AppException.Validation("Customer contact kind must be Mobile or Email.");
}
