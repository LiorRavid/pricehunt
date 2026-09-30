using PriceHunt.Domain;

namespace PriceHunt.Application.Searches;

/// <summary>Stores searches as they progress (the write side).</summary>
public interface ISearchRepository
{
    /// <summary>Stores a new running search and its selected suppliers.</summary>
    /// <param name="search">The search, just started.</param>
    /// <param name="cancellationToken">A token to cancel the write.</param>
    /// <returns>A task that completes when the search is stored.</returns>
    Task AddAsync(Search search, CancellationToken cancellationToken);

    /// <summary>Stores one supplier response.</summary>
    /// <param name="searchId">The search the response belongs to.</param>
    /// <param name="response">The response.</param>
    /// <param name="cancellationToken">A token to cancel the write.</param>
    /// <returns>A task that completes when the response is stored.</returns>
    Task AddResponseAsync(Guid searchId, SupplierResponse response, CancellationToken cancellationToken);

    /// <summary>Stores the search's terminal state and the responses recorded for its silent suppliers, atomically.</summary>
    /// <param name="search">The search, in a terminal state.</param>
    /// <param name="closingResponses">The timed-out or cancelled responses recorded when the search ended.</param>
    /// <param name="cancellationToken">A token to cancel the write.</param>
    /// <returns>A task that completes when the outcome is stored.</returns>
    Task SaveOutcomeAsync(Search search, IReadOnlyCollection<SupplierResponse> closingResponses, CancellationToken cancellationToken);

    /// <summary>Loads every search still marked running, with its responses.</summary>
    /// <param name="cancellationToken">A token to cancel the read.</param>
    /// <returns>The running searches.</returns>
    Task<IReadOnlyList<Search>> GetRunningAsync(CancellationToken cancellationToken);
}
