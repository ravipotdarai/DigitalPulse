using DigitalPulse.Application.Features.Identity;
using DigitalPulse.Contracts.Businesses;
using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;

namespace DigitalPulse.Api.Endpoints;

public static class IdentityEndpoints
{
    public static IEndpointRouteBuilder MapIdentityEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/v1/businesses/{businessId:guid}").WithTags("Identity").RequireAuthorization();
        group.MapGet("/identity", GetAsync);
        group.MapPut("/profile", UpdateProfileAsync);
        group.MapPost("/contacts", AddContactAsync);
        group.MapPut("/contacts/{contactId:guid}", UpdateContactAsync);
        group.MapDelete("/contacts/{contactId:guid}", DeleteContactAsync);
        group.MapPost("/categories", AddCategoryAsync);
        group.MapPut("/categories/{categoryId:guid}", UpdateCategoryAsync);
        group.MapDelete("/categories/{categoryId:guid}", DeleteCategoryAsync);
        group.MapPost("/services", AddServiceAsync);
        group.MapPut("/services/{serviceId:guid}", UpdateServiceAsync);
        group.MapDelete("/services/{serviceId:guid}", DeleteServiceAsync);
        group.MapPost("/brands", AddBrandAsync);
        group.MapDelete("/brands/{linkId:guid}", DeleteBrandAsync);
        group.MapPost("/facts", AddFactAsync);
        group.MapPut("/facts/{factId:guid}", UpdateFactAsync);
        group.MapDelete("/facts/{factId:guid}", DeleteFactAsync);
        group.MapPost("/customers", AddCustomerAsync);
        group.MapPut("/customers/{customerId:guid}", UpdateCustomerAsync);
        group.MapDelete("/customers/{customerId:guid}", DeleteCustomerAsync);
        group.MapPost("/customers/{customerId:guid}/contacts", AddCustomerContactAsync);
        group.MapDelete("/customers/{customerId:guid}/contacts/{contactId:guid}", DeleteCustomerContactAsync);
        return app;
    }

    private static async Task<Ok<BusinessIdentityResponse>> GetAsync(
        Guid businessId,
        GetBusinessIdentityHandler handler,
        CancellationToken cancellationToken) =>
        TypedResults.Ok(await handler.Handle(businessId, cancellationToken));

    private static async Task<Ok<BusinessResponse>> UpdateProfileAsync(
        Guid businessId,
        UpdateBusinessProfileRequest request,
        IValidator<UpdateBusinessProfileRequest> validator,
        UpdateBusinessProfileHandler handler,
        CancellationToken cancellationToken)
    {
        await validator.ValidateAndThrowAsync(request, cancellationToken);
        return TypedResults.Ok(await handler.Handle(businessId, request, cancellationToken));
    }

    private static async Task<Ok<ContactPointResponse>> AddContactAsync(
        Guid businessId,
        ContactPointRequest request,
        IValidator<ContactPointRequest> validator,
        AddContactPointHandler handler,
        CancellationToken cancellationToken)
    {
        await validator.ValidateAndThrowAsync(request, cancellationToken);
        return TypedResults.Ok(await handler.Handle(businessId, request, cancellationToken));
    }

    private static async Task<Ok<ContactPointResponse>> UpdateContactAsync(
        Guid businessId,
        Guid contactId,
        ContactPointRequest request,
        IValidator<ContactPointRequest> validator,
        UpdateContactPointHandler handler,
        CancellationToken cancellationToken)
    {
        await validator.ValidateAndThrowAsync(request, cancellationToken);
        return TypedResults.Ok(await handler.Handle(businessId, contactId, request, cancellationToken));
    }

    private static async Task<NoContent> DeleteContactAsync(
        Guid businessId, Guid contactId, DeleteOwnedHandler handler, CancellationToken cancellationToken)
    {
        await handler.DeleteContact(businessId, contactId, cancellationToken);
        return TypedResults.NoContent();
    }

    private static async Task<Ok<NamedItemResponse>> AddCategoryAsync(
        Guid businessId,
        NamedItemRequest request,
        IValidator<NamedItemRequest> validator,
        AddCategoryHandler handler,
        CancellationToken cancellationToken)
    {
        await validator.ValidateAndThrowAsync(request, cancellationToken);
        return TypedResults.Ok(await handler.Handle(businessId, request, cancellationToken));
    }

    private static async Task<Ok<NamedItemResponse>> UpdateCategoryAsync(
        Guid businessId,
        Guid categoryId,
        NamedItemRequest request,
        IValidator<NamedItemRequest> validator,
        UpdateCategoryHandler handler,
        CancellationToken cancellationToken)
    {
        await validator.ValidateAndThrowAsync(request, cancellationToken);
        return TypedResults.Ok(await handler.Handle(businessId, categoryId, request, cancellationToken));
    }

    private static async Task<NoContent> DeleteCategoryAsync(
        Guid businessId, Guid categoryId, DeleteOwnedHandler handler, CancellationToken cancellationToken)
    {
        await handler.DeleteCategory(businessId, categoryId, cancellationToken);
        return TypedResults.NoContent();
    }

    private static async Task<Ok<NamedItemResponse>> AddServiceAsync(
        Guid businessId,
        NamedItemRequest request,
        IValidator<NamedItemRequest> validator,
        AddServiceHandler handler,
        CancellationToken cancellationToken)
    {
        await validator.ValidateAndThrowAsync(request, cancellationToken);
        return TypedResults.Ok(await handler.Handle(businessId, request, cancellationToken));
    }

    private static async Task<Ok<NamedItemResponse>> UpdateServiceAsync(
        Guid businessId,
        Guid serviceId,
        NamedItemRequest request,
        IValidator<NamedItemRequest> validator,
        UpdateServiceHandler handler,
        CancellationToken cancellationToken)
    {
        await validator.ValidateAndThrowAsync(request, cancellationToken);
        return TypedResults.Ok(await handler.Handle(businessId, serviceId, request, cancellationToken));
    }

    private static async Task<NoContent> DeleteServiceAsync(
        Guid businessId, Guid serviceId, DeleteOwnedHandler handler, CancellationToken cancellationToken)
    {
        await handler.DeleteService(businessId, serviceId, cancellationToken);
        return TypedResults.NoContent();
    }

    private static async Task<Ok<BusinessBrandResponse>> AddBrandAsync(
        Guid businessId,
        NamedItemRequest request,
        IValidator<NamedItemRequest> validator,
        AddBusinessBrandHandler handler,
        CancellationToken cancellationToken)
    {
        await validator.ValidateAndThrowAsync(request, cancellationToken);
        return TypedResults.Ok(await handler.Handle(businessId, request, cancellationToken));
    }

    private static async Task<NoContent> DeleteBrandAsync(
        Guid businessId, Guid linkId, DeleteOwnedHandler handler, CancellationToken cancellationToken)
    {
        await handler.DeleteBrand(businessId, linkId, cancellationToken);
        return TypedResults.NoContent();
    }

    private static async Task<Ok<FactResponse>> AddFactAsync(
        Guid businessId,
        FactRequest request,
        IValidator<FactRequest> validator,
        AddFactHandler handler,
        CancellationToken cancellationToken)
    {
        await validator.ValidateAndThrowAsync(request, cancellationToken);
        return TypedResults.Ok(await handler.Handle(businessId, request, cancellationToken));
    }

    private static async Task<Ok<FactResponse>> UpdateFactAsync(
        Guid businessId,
        Guid factId,
        FactRequest request,
        IValidator<FactRequest> validator,
        UpdateFactHandler handler,
        CancellationToken cancellationToken)
    {
        await validator.ValidateAndThrowAsync(request, cancellationToken);
        return TypedResults.Ok(await handler.Handle(businessId, factId, request, cancellationToken));
    }

    private static async Task<NoContent> DeleteFactAsync(
        Guid businessId, Guid factId, DeleteOwnedHandler handler, CancellationToken cancellationToken)
    {
        await handler.DeleteFact(businessId, factId, cancellationToken);
        return TypedResults.NoContent();
    }

    private static async Task<Ok<CustomerResponse>> AddCustomerAsync(
        Guid businessId,
        CustomerRequest request,
        IValidator<CustomerRequest> validator,
        AddCustomerHandler handler,
        CancellationToken cancellationToken)
    {
        await validator.ValidateAndThrowAsync(request, cancellationToken);
        return TypedResults.Ok(await handler.Handle(businessId, request, cancellationToken));
    }

    private static async Task<Ok<CustomerResponse>> UpdateCustomerAsync(
        Guid businessId,
        Guid customerId,
        CustomerRequest request,
        IValidator<CustomerRequest> validator,
        UpdateCustomerHandler handler,
        CancellationToken cancellationToken)
    {
        await validator.ValidateAndThrowAsync(request, cancellationToken);
        return TypedResults.Ok(await handler.Handle(businessId, customerId, request, cancellationToken));
    }

    private static async Task<NoContent> DeleteCustomerAsync(
        Guid businessId, Guid customerId, DeleteOwnedHandler handler, CancellationToken cancellationToken)
    {
        await handler.DeleteCustomer(businessId, customerId, cancellationToken);
        return TypedResults.NoContent();
    }

    private static async Task<Ok<CustomerContactResponse>> AddCustomerContactAsync(
        Guid businessId,
        Guid customerId,
        CustomerContactRequest request,
        IValidator<CustomerContactRequest> validator,
        AddCustomerContactHandler handler,
        CancellationToken cancellationToken)
    {
        await validator.ValidateAndThrowAsync(request, cancellationToken);
        return TypedResults.Ok(await handler.Handle(businessId, customerId, request, cancellationToken));
    }

    private static async Task<NoContent> DeleteCustomerContactAsync(
        Guid businessId,
        Guid customerId,
        Guid contactId,
        DeleteOwnedHandler handler,
        CancellationToken cancellationToken)
    {
        await handler.DeleteCustomerContact(businessId, customerId, contactId, cancellationToken);
        return TypedResults.NoContent();
    }
}
