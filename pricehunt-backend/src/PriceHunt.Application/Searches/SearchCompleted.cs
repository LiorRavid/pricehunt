using PriceHunt.Domain;

namespace PriceHunt.Application.Searches;

/// <summary>The search has ended; this is always the last event, and is emitted exactly once.</summary>
/// <param name="SearchId">The search id.</param>
/// <param name="Status"><see cref="SearchStatus.Completed"/>, <see cref="SearchStatus.TimedOut"/> or <see cref="SearchStatus.Faulted"/>.</param>
/// <param name="CompletedAt">When the search ended (UTC).</param>
/// <param name="SucceededCount">How many suppliers returned a price.</param>
/// <param name="FailedCount">How many suppliers returned an error.</param>
/// <param name="NoResponseSupplierIds">The suppliers that never responded.</param>
public sealed record SearchCompleted(
    Guid SearchId,
    SearchStatus Status,
    DateTime CompletedAt,
    int SucceededCount,
    int FailedCount,
    IReadOnlyList<SupplierId> NoResponseSupplierIds) : SearchEvent(SearchId);
