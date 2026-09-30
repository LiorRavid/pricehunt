using PriceHunt.Domain;

namespace PriceHunt.Application.Searches;

/// <summary>A supplier failed; the failure is already persisted and affects no other supplier.</summary>
/// <param name="SearchId">The search id.</param>
/// <param name="SupplierId">The supplier.</param>
/// <param name="ErrorCode">A machine-readable error code.</param>
/// <param name="ErrorMessage">A message safe to show users.</param>
/// <param name="ResponseTime">How long the supplier took to fail.</param>
/// <param name="ReceivedAt">When the failure arrived (UTC).</param>
public sealed record SupplierFailed(
    Guid SearchId,
    SupplierId SupplierId,
    string ErrorCode,
    string ErrorMessage,
    TimeSpan ResponseTime,
    DateTime ReceivedAt) : SearchEvent(SearchId);
