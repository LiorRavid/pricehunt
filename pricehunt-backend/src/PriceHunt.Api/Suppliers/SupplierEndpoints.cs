using Microsoft.AspNetCore.Http.HttpResults;
using PriceHunt.Api.Contracts;
using PriceHunt.Application.Suppliers;

namespace PriceHunt.Api.Suppliers;

/// <summary>The supplier catalogue, so the UI never hard-codes suppliers.</summary>
internal static class SupplierEndpoints
{
    public static IEndpointRouteBuilder MapSupplierEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/suppliers", GetSuppliers)
            .WithName("GetSuppliers")
            .WithTags("Suppliers")
            .WithSummary("Lists the suppliers a search can query, in display order.");
        return app;
    }

    private static Ok<SupplierDto[]> GetSuppliers(ISupplierCatalog catalog) =>
        TypedResults.Ok(catalog.Suppliers.Select(supplier => new SupplierDto(supplier.Id.Value, supplier.DisplayName)).ToArray());
}
