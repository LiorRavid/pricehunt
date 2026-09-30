using PriceHunt.Api.Contracts;

namespace PriceHunt.Api.Searches;

/// <summary>The <c>search-started</c> event.</summary>
internal sealed record SearchStartedPayload(
    Guid SearchId,
    IReadOnlyList<SupplierDto> Suppliers,
    DateTime StartedAt,
    DateTime Deadline,
    long MaxDurationMs) : SearchStreamEvent(SearchId);
