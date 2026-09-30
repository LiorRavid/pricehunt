using PriceHunt.Domain;

namespace PriceHunt.Infrastructure.Persistence;

/// <summary>A row in <c>Searches</c>.</summary>
internal sealed class SearchEntity
{
    public Guid Id { get; set; }

    public required string Origin { get; set; }

    public required string OriginNormalized { get; set; }

    public required string Destination { get; set; }

    public required string DestinationNormalized { get; set; }

    public DateOnly ShipDateFrom { get; set; }

    public DateOnly ShipDateTo { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? CompletedAt { get; set; }

    public SearchStatus Status { get; set; }

    public List<SearchSupplierEntity> Suppliers { get; set; } = [];

    public List<SupplierResponseEntity> Responses { get; set; } = [];
}
