using PriceHunt.Domain;

namespace PriceHunt.Infrastructure.Persistence;

/// <summary>A row in <c>SupplierResponses</c>: how one selected supplier's part in a search ended.</summary>
internal sealed class SupplierResponseEntity
{
    public Guid Id { get; set; }

    public Guid SearchId { get; set; }

    public required string SupplierId { get; set; }

    public ResponseOutcome Outcome { get; set; }

    public long? PriceMinorUnits { get; set; }

    public string? Currency { get; set; }

    public int ResponseTimeMs { get; set; }

    public DateTime ReceivedAt { get; set; }

    public string? ErrorCode { get; set; }

    public string? ErrorMessage { get; set; }

    public SearchEntity? Search { get; set; }

    public SupplierEntity? Supplier { get; set; }
}
