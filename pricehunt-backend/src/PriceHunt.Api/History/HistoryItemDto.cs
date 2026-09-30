using PriceHunt.Api.Contracts;
using PriceHunt.Application.History;
using PriceHunt.Domain;

namespace PriceHunt.Api.History;

/// <summary>One row of the price history on the wire.</summary>
internal sealed record HistoryItemDto(
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
    MoneyDto? Price,
    long ResponseTimeMs,
    string? ErrorCode)
{
    public static HistoryItemDto From(PriceHistoryItem item) => new(
        item.Id,
        item.SearchId,
        item.ReceivedAt,
        item.Origin,
        item.Destination,
        item.ShipDateFrom,
        item.ShipDateTo,
        item.SupplierId,
        item.SupplierName,
        item.Outcome,
        item.Price is { } price ? MoneyDto.From(price) : null,
        (long)Math.Round(item.ResponseTime.TotalMilliseconds),
        item.ErrorCode);
}
