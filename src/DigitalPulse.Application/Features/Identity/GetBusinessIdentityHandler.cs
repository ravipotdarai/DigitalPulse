using DigitalPulse.Application.Abstractions;
using DigitalPulse.Contracts.Businesses;
using Microsoft.EntityFrameworkCore;

namespace DigitalPulse.Application.Features.Identity;

public sealed class GetBusinessIdentityHandler
{
    private readonly IAppDbContext _db;
    private readonly ITenantContext _tenantContext;

    public GetBusinessIdentityHandler(IAppDbContext db, ITenantContext tenantContext)
    {
        _db = db;
        _tenantContext = tenantContext;
    }

    public async Task<BusinessIdentityResponse> Handle(Guid businessId, CancellationToken cancellationToken)
    {
        var tenantId = _tenantContext.RequireTenantId();
        var business = await BusinessAccess.RequireAsync(_db, tenantId, businessId, cancellationToken);

        var industries = await _db.Industries.AsNoTracking()
            .OrderBy(x => x.Name)
            .Select(x => new CatalogItemResponse(x.Code, x.Name))
            .ToListAsync(cancellationToken);
        var factTypes = await _db.FactTypes.AsNoTracking()
            .OrderBy(x => x.Name)
            .Select(x => new CatalogItemResponse(x.Code, x.Name))
            .ToListAsync(cancellationToken);
        var contacts = await _db.ContactPoints.AsNoTracking()
            .Where(x => x.BusinessId == businessId)
            .OrderByDescending(x => x.CreatedAtUtc)
            .Select(x => new ContactPointResponse(x.Id, x.BusinessId, x.Kind.ToString(), x.Value, x.Label))
            .ToListAsync(cancellationToken);
        var categories = await _db.Categories.AsNoTracking()
            .Where(x => x.BusinessId == businessId)
            .OrderByDescending(x => x.CreatedAtUtc)
            .Select(x => new NamedItemResponse(x.Id, x.Name))
            .ToListAsync(cancellationToken);
        var services = await _db.Services.AsNoTracking()
            .Where(x => x.BusinessId == businessId)
            .OrderByDescending(x => x.CreatedAtUtc)
            .Select(x => new NamedItemResponse(x.Id, x.Name, x.Description))
            .ToListAsync(cancellationToken);
        var brands = await (
            from link in _db.BusinessBrands.AsNoTracking()
            join brand in _db.Brands.AsNoTracking() on link.BrandId equals brand.Id
            where link.BusinessId == businessId
            orderby link.CreatedAtUtc descending
            select new BusinessBrandResponse(link.Id, brand.Id, brand.Name)
        ).ToListAsync(cancellationToken);
        var facts = await _db.Facts.AsNoTracking()
            .Where(x => x.BusinessId == businessId)
            .OrderByDescending(x => x.CreatedAtUtc)
            .Select(x => new FactResponse(x.Id, x.FactTypeCode, x.Value, x.Status.ToString(), x.Status == Domain.Businesses.FactStatus.Approved))
            .ToListAsync(cancellationToken);
        var customers = await _db.Customers.AsNoTracking()
            .Where(x => x.BusinessId == businessId)
            .OrderByDescending(x => x.CreatedAtUtc)
            .ToListAsync(cancellationToken);
        var customerIds = customers.Select(c => c.Id).ToList();
        var customerContacts = await _db.CustomerContacts.AsNoTracking()
            .Where(x => customerIds.Contains(x.CustomerId))
            .ToListAsync(cancellationToken);

        return new BusinessIdentityResponse(
            business.ToResponse(),
            industries,
            factTypes,
            contacts,
            categories,
            services,
            brands,
            facts,
            customers.Select(customer => new CustomerResponse(
                customer.Id,
                customer.DisplayName,
                customer.Notes,
                customerContacts
                    .Where(c => c.CustomerId == customer.Id)
                    .Select(c => new CustomerContactResponse(c.Id, c.Kind.ToString(), c.Value))
                    .ToList())).ToList());
    }
}
