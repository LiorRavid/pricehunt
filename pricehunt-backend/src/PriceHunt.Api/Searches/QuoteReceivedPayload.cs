using PriceHunt.Api.Contracts;

namespace PriceHunt.Api.Searches;

/// <summary>The <c>quote-received</c> event.</summary>
internal sealed record QuoteReceivedPayload(
    Guid SearchId,
    string SupplierId,
    MoneyDto Price,
    long ResponseTimeMs,
    DateTime ReceivedAt) : SearchStreamEvent(SearchId);
