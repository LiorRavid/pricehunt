namespace PriceHunt.Infrastructure.Persistence;

/// <summary>A row in <c>Suppliers</c>: the configured catalogue, upserted at startup.</summary>
internal sealed class SupplierEntity
{
    public required string Id { get; set; }

    public required string Name { get; set; }
}
