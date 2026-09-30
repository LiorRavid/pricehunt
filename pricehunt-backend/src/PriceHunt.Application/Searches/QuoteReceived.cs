using PriceHunt.Domain;

namespace PriceHunt.Application.Searches;

/// <summary>A supplier returned a price; it is already persisted.</summary>
/// <param name="SearchId">The search id.</param>
/// <param name="SupplierId">The supplier.</param>
/// <param name="Price">The quoted price.</param>
/// <param name="ResponseTime">How long the supplier took.</param>
/// <param name="ReceivedAt">When the price arrived (UTC).</param>
public sealed record QuoteReceived(
    Guid SearchId,
    SupplierId SupplierId,
    Money Price,
    TimeSpan ResponseTime,
    DateTime ReceivedAt) : SearchEvent(SearchId);
