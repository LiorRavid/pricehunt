using PriceHunt.Application.Searches;
using PriceHunt.Domain;

namespace PriceHunt.Api.Tests.Fakes;

/// <summary>
/// Wraps the real repository and signals when a search's outcome is saved, so tests await the
/// server's finalisation instead of polling the database.
/// </summary>
internal sealed class ObservedSearchRepository(ISearchRepository inner) : ISearchRepository
{
    private readonly TaskCompletionSource<SearchStatus> _outcomeSaved = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public Task<SearchStatus> OutcomeSaved => _outcomeSaved.Task;

    public Task AddAsync(Search search, CancellationToken cancellationToken) => inner.AddAsync(search, cancellationToken);

    public Task AddResponseAsync(Guid searchId, SupplierResponse response, CancellationToken cancellationToken) =>
        inner.AddResponseAsync(searchId, response, cancellationToken);

    public async Task SaveOutcomeAsync(Search search, IReadOnlyCollection<SupplierResponse> closingResponses, CancellationToken cancellationToken)
    {
        await inner.SaveOutcomeAsync(search, closingResponses, cancellationToken);
        _outcomeSaved.TrySetResult(search.Status);
    }

    public Task<IReadOnlyList<Search>> GetRunningAsync(CancellationToken cancellationToken) => inner.GetRunningAsync(cancellationToken);
}
