using Microsoft.Extensions.Logging;
using PriceHunt.Domain;

namespace PriceHunt.Application.Searches;

/// <summary>
/// Closes searches that a crash or shutdown left <see cref="SearchStatus.Running"/>, as cancelled,
/// recording their silent suppliers as cancelled too. Runs once at startup.
/// </summary>
/// <param name="repository">Where searches are persisted.</param>
/// <param name="timeProvider">The clock for the closing timestamp.</param>
/// <param name="logger">The search log.</param>
public sealed class InterruptedSearchRecovery(
    ISearchRepository repository,
    TimeProvider timeProvider,
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
            DateTime now = timeProvider.GetUtcNow().UtcDateTime;
            IReadOnlyList<SupplierResponse> closing = search.Cancel(now < search.CreatedAt ? search.CreatedAt : now);
            await repository.SaveOutcomeAsync(search, closing, cancellationToken).ConfigureAwait(false);
        }

        if (running.Count > 0)
        {
            SearchLog.InterruptedSearchesRecovered(logger, running.Count);
        }

        return running.Count;
    }
}
