using PriceHunt.Domain;

namespace PriceHunt.Application.History;

/// <summary>One supplier response in the price history, with its search's route and shipping dates.</summary>
/// <param name="Id">The response id.</param>
/// <param name="SearchId">The search the response belongs to.</param>
/// <param name="ReceivedAt">When the outcome was recorded (UTC).</param>
/// <param name="Origin">Where the goods ship from.</param>
/// <param name="Destination">Where the goods ship to.</param>
/// <param name="ShipDateFrom">The first shipping day searched.</param>
/// <param name="ShipDateTo">The last shipping day searched.</param>
/// <param name="SupplierId">The supplier id.</param>
/// <param name="SupplierName">The supplier's display name.</param>
/// <param name="Outcome">How the supplier's part ended.</param>
/// <param name="Price">The quoted price, for a success only.</param>
/// <param name="ResponseTime">How long the supplier took or was waited for.</param>
/// <param name="ErrorCode">The error code, for a failure only.</param>
public sealed record PriceHistoryItem(
    Guid Id,
    Guid SearchId,
    DateTime ReceivedAt,
    string Origin,
    string Destination,
    DateOnly ShipDateFrom,
    DateOnly ShipDateTo,
    string SupplierId,
    string SupplierName,
    ResponseOutcome Outcome,
    Money? Price,
    TimeSpan ResponseTime,
    string? ErrorCode);
