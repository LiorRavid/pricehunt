namespace PriceHunt.Infrastructure.Persistence;

/// <summary>A row in <c>SearchSuppliers</c>: one supplier selected for a search.</summary>
internal sealed class SearchSupplierEntity
{
    public Guid SearchId { get; set; }

    public required string SupplierId { get; set; }
}
