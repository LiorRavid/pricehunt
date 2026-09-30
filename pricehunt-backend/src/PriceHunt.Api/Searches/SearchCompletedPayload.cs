using PriceHunt.Domain;

namespace PriceHunt.Api.Searches;

/// <summary>The terminal <c>search-completed</c> event: always last, exactly once.</summary>
internal sealed record SearchCompletedPayload(
    Guid SearchId,
    SearchStatus Status,
    DateTime CompletedAt,
    int RespondedCount,
    int SucceededCount,
    int FailedCount,
    IReadOnlyList<string> NoResponseSupplierIds) : SearchStreamEvent(SearchId);
