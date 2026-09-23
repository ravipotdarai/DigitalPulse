using DigitalPulse.Application.Abstractions;
using DigitalPulse.Application.Common;
using DigitalPulse.Contracts.Businesses;
using DigitalPulse.Domain.Businesses;
using Microsoft.EntityFrameworkCore;

namespace DigitalPulse.Application.Features.Identity;

public sealed class UpdateBusinessProfileHandler
{
    private readonly IAppDbContext _db;
    private readonly ITenantContext _tenants;

    public UpdateBusinessProfileHandler(IAppDbContext db, ITenantContext tenants)
    {
        _db = db;
        _tenants = tenants;
    }

    public async Task<BusinessResponse> Handle(Guid businessId, UpdateBusinessProfileRequest request, CancellationToken cancellationToken)
    {
        var tenantId = _tenants.RequireTenantId();
        var business = await BusinessAccess.RequireAsync(_db, tenantId, businessId, cancellationToken);
        if (!string.IsNullOrWhiteSpace(request.IndustryCode)
            && !await _db.Industries.AnyAsync(i => i.Code == request.IndustryCode.Trim().ToUpperInvariant(), cancellationToken))
        {
            throw AppException.Validation("Industry is not in the catalog.");
        }

        business.UpdateIdentity(request.Name, request.Website, request.FoundedYear, request.BrandVoice, request.IndustryCode);
        await _db.SaveChangesAsync(cancellationToken);
        return business.ToResponse();
    }
}

public sealed class AddContactPointHandler
{
    private readonly IAppDbContext _db;
    private readonly ITenantContext _tenants;

    public AddContactPointHandler(IAppDbContext db, ITenantContext tenants)
    {
        _db = db;
        _tenants = tenants;
    }

    public async Task<ContactPointResponse> Handle(Guid businessId, ContactPointRequest request, CancellationToken cancellationToken)
    {
        var tenantId = _tenants.RequireTenantId();
        await BusinessAccess.RequireAsync(_db, tenantId, businessId, cancellationToken);
        var kind = BusinessAccess.ParseContactKind(request.Kind);
        if (await _db.ContactPoints.AnyAsync(c => c.BusinessId == businessId && c.Kind == kind && c.Value == request.Value.Trim(), cancellationToken))
        {
            throw AppException.Conflict("That contact already exists.");
        }

        var contact = ContactPoint.Create(tenantId, businessId, kind, request.Value, request.Label);
        _db.ContactPoints.Add(contact);
        await _db.SaveChangesAsync(cancellationToken);
        return new ContactPointResponse(contact.Id, contact.BusinessId, contact.Kind.ToString(), contact.Value, contact.Label);
    }
}

public sealed class UpdateContactPointHandler
{
    private readonly IAppDbContext _db;
    private readonly ITenantContext _tenants;

    public UpdateContactPointHandler(IAppDbContext db, ITenantContext tenants)
    {
        _db = db;
        _tenants = tenants;
    }

    public async Task<ContactPointResponse> Handle(Guid businessId, Guid contactId, ContactPointRequest request, CancellationToken cancellationToken)
    {
        var tenantId = _tenants.RequireTenantId();
        await BusinessAccess.RequireAsync(_db, tenantId, businessId, cancellationToken);
        var contact = await _db.ContactPoints.FirstOrDefaultAsync(c => c.Id == contactId && c.BusinessId == businessId, cancellationToken)
            ?? throw AppException.NotFound("Contact was not found.");
        contact.Update(BusinessAccess.ParseContactKind(request.Kind), request.Value, request.Label);
        await _db.SaveChangesAsync(cancellationToken);
        return new ContactPointResponse(contact.Id, contact.BusinessId, contact.Kind.ToString(), contact.Value, contact.Label);
    }
}

public sealed class DeleteOwnedHandler
{
    private readonly IAppDbContext _db;
    private readonly ITenantContext _tenants;

    public DeleteOwnedHandler(IAppDbContext db, ITenantContext tenants)
    {
        _db = db;
        _tenants = tenants;
    }

    public async Task DeleteContact(Guid businessId, Guid contactId, CancellationToken cancellationToken)
    {
        var tenantId = _tenants.RequireTenantId();
        await BusinessAccess.RequireAsync(_db, tenantId, businessId, cancellationToken);
        var contact = await _db.ContactPoints.FirstOrDefaultAsync(c => c.Id == contactId && c.BusinessId == businessId, cancellationToken)
            ?? throw AppException.NotFound("Contact was not found.");
        _db.ContactPoints.Remove(contact);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteCategory(Guid businessId, Guid categoryId, CancellationToken cancellationToken)
    {
        var tenantId = _tenants.RequireTenantId();
        await BusinessAccess.RequireAsync(_db, tenantId, businessId, cancellationToken);
        var item = await _db.Categories.FirstOrDefaultAsync(c => c.Id == categoryId && c.BusinessId == businessId, cancellationToken)
            ?? throw AppException.NotFound("Category was not found.");
        _db.Categories.Remove(item);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteService(Guid businessId, Guid serviceId, CancellationToken cancellationToken)
    {
        var tenantId = _tenants.RequireTenantId();
        await BusinessAccess.RequireAsync(_db, tenantId, businessId, cancellationToken);
        var item = await _db.Services.FirstOrDefaultAsync(s => s.Id == serviceId && s.BusinessId == businessId, cancellationToken)
            ?? throw AppException.NotFound("Service was not found.");
        _db.Services.Remove(item);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteBrand(Guid businessId, Guid linkId, CancellationToken cancellationToken)
    {
        var tenantId = _tenants.RequireTenantId();
        await BusinessAccess.RequireAsync(_db, tenantId, businessId, cancellationToken);
        var link = await _db.BusinessBrands.FirstOrDefaultAsync(b => b.Id == linkId && b.BusinessId == businessId, cancellationToken)
            ?? throw AppException.NotFound("Brand was not found.");
        _db.BusinessBrands.Remove(link);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteFact(Guid businessId, Guid factId, CancellationToken cancellationToken)
    {
        var tenantId = _tenants.RequireTenantId();
        await BusinessAccess.RequireAsync(_db, tenantId, businessId, cancellationToken);
        var fact = await _db.Facts.FirstOrDefaultAsync(f => f.Id == factId && f.BusinessId == businessId, cancellationToken)
            ?? throw AppException.NotFound("Fact was not found.");
        _db.Facts.Remove(fact);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteCustomer(Guid businessId, Guid customerId, CancellationToken cancellationToken)
    {
        var tenantId = _tenants.RequireTenantId();
        await BusinessAccess.RequireAsync(_db, tenantId, businessId, cancellationToken);
        var customer = await _db.Customers.FirstOrDefaultAsync(c => c.Id == customerId && c.BusinessId == businessId, cancellationToken)
            ?? throw AppException.NotFound("Customer was not found.");
        var contacts = await _db.CustomerContacts.Where(c => c.CustomerId == customerId).ToListAsync(cancellationToken);
        _db.CustomerContacts.RemoveRange(contacts);
        _db.Customers.Remove(customer);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteCustomerContact(Guid businessId, Guid customerId, Guid contactId, CancellationToken cancellationToken)
    {
        var tenantId = _tenants.RequireTenantId();
        await BusinessAccess.RequireAsync(_db, tenantId, businessId, cancellationToken);
        var contact = await _db.CustomerContacts.FirstOrDefaultAsync(
            c => c.Id == contactId && c.CustomerId == customerId && c.BusinessId == businessId, cancellationToken)
            ?? throw AppException.NotFound("Customer contact was not found.");
        _db.CustomerContacts.Remove(contact);
        await _db.SaveChangesAsync(cancellationToken);
    }
}

public sealed class AddCategoryHandler
{
    private readonly IAppDbContext _db;
    private readonly ITenantContext _tenants;

    public AddCategoryHandler(IAppDbContext db, ITenantContext tenants)
    {
        _db = db;
        _tenants = tenants;
    }

    public async Task<NamedItemResponse> Handle(Guid businessId, NamedItemRequest request, CancellationToken cancellationToken)
    {
        var tenantId = _tenants.RequireTenantId();
        await BusinessAccess.RequireAsync(_db, tenantId, businessId, cancellationToken);
        if (await _db.Categories.AnyAsync(c => c.BusinessId == businessId && c.Name == request.Name.Trim(), cancellationToken))
        {
            throw AppException.Conflict("That category is already on this business.");
        }

        var category = BusinessCategory.Create(tenantId, businessId, request.Name);
        _db.Categories.Add(category);
        await _db.SaveChangesAsync(cancellationToken);
        return new NamedItemResponse(category.Id, category.Name);
    }
}

public sealed class UpdateCategoryHandler
{
    private readonly IAppDbContext _db;
    private readonly ITenantContext _tenants;

    public UpdateCategoryHandler(IAppDbContext db, ITenantContext tenants)
    {
        _db = db;
        _tenants = tenants;
    }

    public async Task<NamedItemResponse> Handle(Guid businessId, Guid categoryId, NamedItemRequest request, CancellationToken cancellationToken)
    {
        var tenantId = _tenants.RequireTenantId();
        await BusinessAccess.RequireAsync(_db, tenantId, businessId, cancellationToken);
        var category = await _db.Categories.FirstOrDefaultAsync(c => c.Id == categoryId && c.BusinessId == businessId, cancellationToken)
            ?? throw AppException.NotFound("Category was not found.");
        if (await _db.Categories.AnyAsync(c => c.BusinessId == businessId && c.Name == request.Name.Trim() && c.Id != categoryId, cancellationToken))
        {
            throw AppException.Conflict("That category is already on this business.");
        }

        category.Update(request.Name);
        await _db.SaveChangesAsync(cancellationToken);
        return new NamedItemResponse(category.Id, category.Name);
    }
}

public sealed class AddServiceHandler
{
    private readonly IAppDbContext _db;
    private readonly ITenantContext _tenants;

    public AddServiceHandler(IAppDbContext db, ITenantContext tenants)
    {
        _db = db;
        _tenants = tenants;
    }

    public async Task<NamedItemResponse> Handle(Guid businessId, NamedItemRequest request, CancellationToken cancellationToken)
    {
        var tenantId = _tenants.RequireTenantId();
        await BusinessAccess.RequireAsync(_db, tenantId, businessId, cancellationToken);
        var service = Service.Create(tenantId, businessId, request.Name, request.Description);
        _db.Services.Add(service);
        await _db.SaveChangesAsync(cancellationToken);
        return new NamedItemResponse(service.Id, service.Name, service.Description);
    }
}

public sealed class UpdateServiceHandler
{
    private readonly IAppDbContext _db;
    private readonly ITenantContext _tenants;

    public UpdateServiceHandler(IAppDbContext db, ITenantContext tenants)
    {
        _db = db;
        _tenants = tenants;
    }

    public async Task<NamedItemResponse> Handle(Guid businessId, Guid serviceId, NamedItemRequest request, CancellationToken cancellationToken)
    {
        var tenantId = _tenants.RequireTenantId();
        await BusinessAccess.RequireAsync(_db, tenantId, businessId, cancellationToken);
        var service = await _db.Services.FirstOrDefaultAsync(s => s.Id == serviceId && s.BusinessId == businessId, cancellationToken)
            ?? throw AppException.NotFound("Service was not found.");
        service.Update(request.Name, request.Description);
        await _db.SaveChangesAsync(cancellationToken);
        return new NamedItemResponse(service.Id, service.Name, service.Description);
    }
}

public sealed class AddBusinessBrandHandler
{
    private readonly IAppDbContext _db;
    private readonly ITenantContext _tenants;

    public AddBusinessBrandHandler(IAppDbContext db, ITenantContext tenants)
    {
        _db = db;
        _tenants = tenants;
    }

    public async Task<BusinessBrandResponse> Handle(Guid businessId, NamedItemRequest request, CancellationToken cancellationToken)
    {
        var tenantId = _tenants.RequireTenantId();
        await BusinessAccess.RequireAsync(_db, tenantId, businessId, cancellationToken);
        var name = request.Name.Trim();
        var brand = await _db.Brands.FirstOrDefaultAsync(b => b.TenantId == tenantId && b.Name == name, cancellationToken);
        if (brand is null)
        {
            brand = Brand.Create(tenantId, name);
            _db.Brands.Add(brand);
            await _db.SaveChangesAsync(cancellationToken);
        }

        if (await _db.BusinessBrands.AnyAsync(l => l.BusinessId == businessId && l.BrandId == brand.Id, cancellationToken))
        {
            throw AppException.Conflict("That brand is already linked.");
        }

        var link = BusinessBrand.Link(tenantId, businessId, brand.Id);
        _db.BusinessBrands.Add(link);
        await _db.SaveChangesAsync(cancellationToken);
        return new BusinessBrandResponse(link.Id, brand.Id, brand.Name);
    }
}

public sealed class AddFactHandler
{
    private readonly IAppDbContext _db;
    private readonly ITenantContext _tenants;

    public AddFactHandler(IAppDbContext db, ITenantContext tenants)
    {
        _db = db;
        _tenants = tenants;
    }

    public async Task<FactResponse> Handle(Guid businessId, FactRequest request, CancellationToken cancellationToken)
    {
        var tenantId = _tenants.RequireTenantId();
        await BusinessAccess.RequireAsync(_db, tenantId, businessId, cancellationToken);
        var code = request.FactTypeCode.Trim().ToUpperInvariant();
        if (!await _db.FactTypes.AnyAsync(f => f.Code == code, cancellationToken))
        {
            throw AppException.Validation("Fact type is not in the catalog.");
        }

        var fact = BusinessFact.Create(tenantId, businessId, code, request.Value, BusinessAccess.ParseFactStatus(request.Status));
        _db.Facts.Add(fact);
        await _db.SaveChangesAsync(cancellationToken);
        return new FactResponse(fact.Id, fact.FactTypeCode, fact.Value, fact.Status.ToString(), fact.CanPublish);
    }
}

public sealed class UpdateFactHandler
{
    private readonly IAppDbContext _db;
    private readonly ITenantContext _tenants;

    public UpdateFactHandler(IAppDbContext db, ITenantContext tenants)
    {
        _db = db;
        _tenants = tenants;
    }

    public async Task<FactResponse> Handle(Guid businessId, Guid factId, FactRequest request, CancellationToken cancellationToken)
    {
        var tenantId = _tenants.RequireTenantId();
        await BusinessAccess.RequireAsync(_db, tenantId, businessId, cancellationToken);
        var fact = await _db.Facts.FirstOrDefaultAsync(f => f.Id == factId && f.BusinessId == businessId, cancellationToken)
            ?? throw AppException.NotFound("Fact was not found.");
        var code = request.FactTypeCode.Trim().ToUpperInvariant();
        if (!await _db.FactTypes.AnyAsync(f => f.Code == code, cancellationToken))
        {
            throw AppException.Validation("Fact type is not in the catalog.");
        }

        fact.Update(code, request.Value, BusinessAccess.ParseFactStatus(request.Status));
        await _db.SaveChangesAsync(cancellationToken);
        return new FactResponse(fact.Id, fact.FactTypeCode, fact.Value, fact.Status.ToString(), fact.CanPublish);
    }
}

public sealed class AddCustomerHandler
{
    private readonly IAppDbContext _db;
    private readonly ITenantContext _tenants;

    public AddCustomerHandler(IAppDbContext db, ITenantContext tenants)
    {
        _db = db;
        _tenants = tenants;
    }

    public async Task<CustomerResponse> Handle(Guid businessId, CustomerRequest request, CancellationToken cancellationToken)
    {
        var tenantId = _tenants.RequireTenantId();
        await BusinessAccess.RequireAsync(_db, tenantId, businessId, cancellationToken);
        var customer = Customer.Create(tenantId, businessId, request.DisplayName, request.Notes);
        _db.Customers.Add(customer);
        await _db.SaveChangesAsync(cancellationToken);

        var mobile = customer.AddContact(CustomerContactKind.Mobile, request.Mobile);
        _db.CustomerContacts.Add(mobile);
        var contacts = new List<CustomerContactResponse>
        {
            new(mobile.Id, mobile.Kind.ToString(), mobile.Value)
        };
        if (!string.IsNullOrWhiteSpace(request.Email))
        {
            var email = customer.AddContact(CustomerContactKind.Email, request.Email);
            _db.CustomerContacts.Add(email);
            contacts.Add(new CustomerContactResponse(email.Id, email.Kind.ToString(), email.Value));
        }

        await _db.SaveChangesAsync(cancellationToken);
        return new CustomerResponse(customer.Id, customer.DisplayName, customer.Notes, contacts);
    }
}

public sealed class UpdateCustomerHandler
{
    private readonly IAppDbContext _db;
    private readonly ITenantContext _tenants;

    public UpdateCustomerHandler(IAppDbContext db, ITenantContext tenants)
    {
        _db = db;
        _tenants = tenants;
    }

    public async Task<CustomerResponse> Handle(Guid businessId, Guid customerId, CustomerRequest request, CancellationToken cancellationToken)
    {
        var tenantId = _tenants.RequireTenantId();
        await BusinessAccess.RequireAsync(_db, tenantId, businessId, cancellationToken);
        var customer = await _db.Customers.FirstOrDefaultAsync(c => c.Id == customerId && c.BusinessId == businessId, cancellationToken)
            ?? throw AppException.NotFound("Customer was not found.");
        customer.Update(request.DisplayName, request.Notes);

        var contacts = await _db.CustomerContacts.Where(c => c.CustomerId == customerId).ToListAsync(cancellationToken);
        var mobile = contacts.FirstOrDefault(c => c.Kind == CustomerContactKind.Mobile);
        if (mobile is null)
        {
            mobile = customer.AddContact(CustomerContactKind.Mobile, request.Mobile);
            _db.CustomerContacts.Add(mobile);
            contacts.Add(mobile);
        }
        else
        {
            mobile.Update(CustomerContactKind.Mobile, request.Mobile);
        }

        var email = contacts.FirstOrDefault(c => c.Kind == CustomerContactKind.Email);
        if (!string.IsNullOrWhiteSpace(request.Email))
        {
            if (email is null)
            {
                email = customer.AddContact(CustomerContactKind.Email, request.Email);
                _db.CustomerContacts.Add(email);
                contacts.Add(email);
            }
            else
            {
                email.Update(CustomerContactKind.Email, request.Email);
            }
        }
        else if (email is not null)
        {
            _db.CustomerContacts.Remove(email);
            contacts.Remove(email);
        }

        await _db.SaveChangesAsync(cancellationToken);
        return new CustomerResponse(
            customer.Id,
            customer.DisplayName,
            customer.Notes,
            contacts.Select(c => new CustomerContactResponse(c.Id, c.Kind.ToString(), c.Value)).ToList());
    }
}

public sealed class AddCustomerContactHandler
{
    private readonly IAppDbContext _db;
    private readonly ITenantContext _tenants;

    public AddCustomerContactHandler(IAppDbContext db, ITenantContext tenants)
    {
        _db = db;
        _tenants = tenants;
    }

    public async Task<CustomerContactResponse> Handle(
        Guid businessId,
        Guid customerId,
        CustomerContactRequest request,
        CancellationToken cancellationToken)
    {
        var tenantId = _tenants.RequireTenantId();
        await BusinessAccess.RequireAsync(_db, tenantId, businessId, cancellationToken);
        var customer = await _db.Customers.FirstOrDefaultAsync(c => c.Id == customerId && c.BusinessId == businessId, cancellationToken)
            ?? throw AppException.NotFound("Customer was not found.");
        var contact = customer.AddContact(BusinessAccess.ParseCustomerContactKind(request.Kind), request.Value);
        _db.CustomerContacts.Add(contact);
        await _db.SaveChangesAsync(cancellationToken);
        return new CustomerContactResponse(contact.Id, contact.Kind.ToString(), contact.Value);
    }
}
