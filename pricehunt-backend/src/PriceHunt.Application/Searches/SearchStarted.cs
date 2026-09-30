namespace PriceHunt.Application.Searches;

/// <summary>The search is persisted and its supplier calls are running.</summary>
/// <param name="SearchId">The search id.</param>
/// <param name="Suppliers">The suppliers being queried, in catalogue order.</param>
/// <param name="StartedAt">When the search started (UTC).</param>
/// <param name="Deadline">When the search will end at the latest (UTC).</param>
/// <param name="MaxDuration">The search's time budget.</param>
public sealed record SearchStarted(
    Guid SearchId,
    IReadOnlyList<SupplierSummary> Suppliers,
    DateTime StartedAt,
    DateTime Deadline,
    TimeSpan MaxDuration) : SearchEvent(SearchId);
