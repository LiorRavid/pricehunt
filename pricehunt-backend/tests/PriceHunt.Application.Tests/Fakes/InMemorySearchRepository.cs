using PriceHunt.Application.Searches;
using PriceHunt.Domain;

namespace PriceHunt.Application.Tests.Fakes;

/// <summary>Records every write, and whether its token was already cancelled at the time.</summary>
internal sealed class InMemorySearchRepository : ISearchRepository
{
    public List<Search> Added { get; } = [];

    public List<SupplierResponse> Responses { get; } = [];

    public List<SavedOutcome> Outcomes { get; } = [];

    public List<Search> Running { get; } = [];

    public Exception? FailResponseWritesWith { get; set; }

    public Exception? FailOutcomeWritesWith { get; set; }

    public Task AddAsync(Search search, CancellationToken cancellationToken)
    {
        Added.Add(search);
        return Task.CompletedTask;
    }

    public Task AddResponseAsync(Guid searchId, SupplierResponse response, CancellationToken cancellationToken)
    {
        if (FailResponseWritesWith is { } exception)
        {
            return Task.FromException(exception);
        }

        Responses.Add(response);
        return Task.CompletedTask;
    }

    public Task SaveOutcomeAsync(Search search, IReadOnlyCollection<SupplierResponse> closingResponses, CancellationToken cancellationToken)
    {
        if (FailOutcomeWritesWith is { } exception)
        {
            return Task.FromException(exception);
        }

        Outcomes.Add(new SavedOutcome(search.Id, search.Status, search.CompletedAt, [.. closingResponses], cancellationToken.IsCancellationRequested));
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<Search>> GetRunningAsync(CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<Search>>([.. Running]);
}

internal sealed record SavedOutcome(
    Guid SearchId,
    SearchStatus Status,
    DateTime? CompletedAt,
    IReadOnlyList<SupplierResponse> ClosingResponses,
    bool TokenWasCancelled);
