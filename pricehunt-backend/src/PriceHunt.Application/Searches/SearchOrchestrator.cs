using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace PriceHunt.Application.Searches;

/// <summary>
/// Runs a search and streams its events as they happen: <see cref="SearchStarted"/> first, then
/// each supplier outcome in completion order, then exactly one <see cref="SearchCompleted"/>.
/// The search ends at its deadline (SV3) or when <c>cancellationToken</c> fires (SV5).
/// </summary>
/// <param name="repository">Where the search and its responses are persisted.</param>
/// <param name="timeProvider">The clock for timestamps, response times and the deadline.</param>
/// <param name="options">The search limits.</param>
/// <param name="logger">The search log.</param>
public sealed class SearchOrchestrator(
    ISearchRepository repository,
    TimeProvider timeProvider,
    IOptions<SearchOptions> options,
    ILogger<SearchOrchestrator> logger)
{
    /// <summary>Runs <paramref name="plan"/> and streams its events.</summary>
    /// <param name="plan">A validated plan from <see cref="SearchPlanner"/>.</param>
    /// <param name="cancellationToken">The caller's token; cancelling it cancels every outstanding supplier call.</param>
    /// <returns>The search events, in order.</returns>
    public async IAsyncEnumerable<SearchEvent> RunAsync(
        SearchPlan plan,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(plan);

        SearchRun run = await SearchRun.StartAsync(plan, repository, timeProvider, options.Value, logger, cancellationToken)
            .ConfigureAwait(false);
        await using (run.ConfigureAwait(false))
        {
            yield return run.Started;
            while (await run.NextAsync().ConfigureAwait(false) is { } searchEvent)
            {
                yield return searchEvent;
            }
        }
    }
}
