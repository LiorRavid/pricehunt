using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PriceHunt.Domain;

namespace PriceHunt.Application.Searches;

/// <summary>
/// Closes searches that a crash or shutdown left <see cref="SearchStatus.Running"/>, as cancelled,
/// recording their silent suppliers as cancelled too. Runs once at startup.
/// </summary>
/// <param name="repository">Where searches are persisted.</param>
/// <param name="timeProvider">The clock for the closing timestamp.</param>
/// <param name="options">The search limits; no search outlives <see cref="SearchOptions.MaxDuration"/>.</param>
/// <param name="logger">The search log.</param>
public sealed class InterruptedSearchRecovery(
    ISearchRepository repository,
    TimeProvider timeProvider,
    IOptions<SearchOptions> options,
    ILogger<InterruptedSearchRecovery> logger)
{
    /// <summary>Closes every search still marked running.</summary>
    /// <param name="cancellationToken">A token to cancel the recovery.</param>
    /// <returns>How many searches were closed.</returns>
    public async Task<int> RecoverAsync(CancellationToken cancellationToken)
    {
        IReadOnlyList<Search> running = await repository.GetRunningAsync(cancellationToken).ConfigureAwait(false);
        foreach (Search search in running)
        {
            // A search ended by its deadline at the latest, so the downtime never becomes a response
            // time; and one whose suppliers had all answered simply completed.
            DateTime at = ClosingTime(search, timeProvider.GetUtcNow().UtcDateTime);
            IReadOnlyList<SupplierResponse> closing = [];
            if (search.HasPendingSuppliers)
            {
                closing = search.Cancel(at);
            }
            else
            {
                search.Complete(at);
            }

            await repository.SaveOutcomeAsync(search, closing, cancellationToken).ConfigureAwait(false);
        }

        if (running.Count > 0)
        {
            SearchLog.InterruptedSearchesRecovered(logger, running.Count);
        }

        return running.Count;
    }

    private DateTime ClosingTime(Search search, DateTime now)
    {
        DateTime deadline = search.CreatedAt + options.Value.MaxDuration;
        if (now < search.CreatedAt)
        {
            return search.CreatedAt;
        }

        return now > deadline ? deadline : now;
    }
}
