namespace PriceHunt.Domain;

/// <summary>
/// One search across the selected suppliers. It starts <see cref="SearchStatus.Running"/>,
/// accepts exactly one response per selected supplier, and reaches exactly one terminal state.
/// </summary>
public sealed class Search
{
    private readonly List<SupplierResponse> _responses = [];
    private readonly HashSet<SupplierId> _answered = [];

    private Search(Guid id, SearchCriteria criteria, IReadOnlyList<SupplierId> selectedSuppliers, DateTime createdAt)
    {
        ArgumentNullException.ThrowIfNull(criteria);
        ArgumentNullException.ThrowIfNull(selectedSuppliers);
        if (selectedSuppliers.Count == 0)
        {
            throw new ArgumentException("A search needs at least one supplier.", nameof(selectedSuppliers));
        }

        if (selectedSuppliers.Distinct().Count() != selectedSuppliers.Count)
        {
            throw new ArgumentException("A supplier can be selected only once.", nameof(selectedSuppliers));
        }

        Id = id;
        Criteria = criteria;
        SelectedSuppliers = selectedSuppliers;
        CreatedAt = UtcTimestamp.Require(createdAt, nameof(createdAt));
        Status = SearchStatus.Running;
    }

    /// <summary>Gets the search's time-ordered identifier.</summary>
    public Guid Id { get; }

    /// <summary>Gets what the search asks for.</summary>
    public SearchCriteria Criteria { get; }

    /// <summary>Gets the suppliers to query, in selection order.</summary>
    public IReadOnlyList<SupplierId> SelectedSuppliers { get; }

    /// <summary>Gets when the search started (UTC).</summary>
    public DateTime CreatedAt { get; }

    /// <summary>Gets when the search reached its terminal state (UTC), if it has.</summary>
    public DateTime? CompletedAt { get; private set; }

    /// <summary>Gets where the search is in its lifecycle.</summary>
    public SearchStatus Status { get; private set; }

    /// <summary>Gets the recorded responses, in the order they were recorded.</summary>
    public IReadOnlyList<SupplierResponse> Responses => _responses;

    /// <summary>Gets the selected suppliers without a recorded response, in selection order.</summary>
    public IReadOnlyList<SupplierId> PendingSuppliers => [.. SelectedSuppliers.Where(id => !_answered.Contains(id))];

    /// <summary>Gets a value indicating whether any selected supplier has no recorded response.</summary>
    public bool HasPendingSuppliers => _answered.Count < SelectedSuppliers.Count;

    /// <summary>Starts a search.</summary>
    /// <param name="criteria">What the search asks for.</param>
    /// <param name="suppliers">The suppliers to query; at least one, each once.</param>
    /// <param name="createdAt">When the search starts (UTC).</param>
    /// <returns>A running search.</returns>
    public static Search Start(SearchCriteria criteria, IEnumerable<SupplierId> suppliers, DateTime createdAt)
    {
        ArgumentNullException.ThrowIfNull(suppliers);
        var id = Guid.CreateVersion7(new DateTimeOffset(UtcTimestamp.Require(createdAt, nameof(createdAt))));
        return new Search(id, criteria, [.. suppliers], createdAt);
    }

    /// <summary>Restores a persisted search with its recorded responses.</summary>
    /// <param name="id">The search id.</param>
    /// <param name="criteria">What the search asked for.</param>
    /// <param name="suppliers">The selected suppliers.</param>
    /// <param name="createdAt">When the search started (UTC).</param>
    /// <param name="status">The persisted status.</param>
    /// <param name="completedAt">When the search ended (UTC), if it has.</param>
    /// <param name="responses">The recorded responses.</param>
    /// <returns>The search.</returns>
    public static Search Restore(
        Guid id,
        SearchCriteria criteria,
        IEnumerable<SupplierId> suppliers,
        DateTime createdAt,
        SearchStatus status,
        DateTime? completedAt,
        IEnumerable<SupplierResponse> responses)
    {
        ArgumentNullException.ThrowIfNull(suppliers);
        ArgumentNullException.ThrowIfNull(responses);

        var search = new Search(id, criteria, [.. suppliers], createdAt);
        foreach (SupplierResponse response in responses)
        {
            search.Add(response);
        }

        search.Status = status;
        search.CompletedAt = completedAt is { } at ? UtcTimestamp.Require(at, nameof(completedAt)) : null;
        return search;
    }

    /// <summary>Records a supplier's price.</summary>
    /// <param name="supplierId">A selected supplier without a response yet.</param>
    /// <param name="price">The quoted price.</param>
    /// <param name="responseTime">How long the supplier took.</param>
    /// <param name="receivedAt">When the price arrived (UTC).</param>
    /// <returns>The recorded response.</returns>
    public SupplierResponse RecordQuote(SupplierId supplierId, Money price, TimeSpan responseTime, DateTime receivedAt) =>
        Add(SupplierResponse.Succeeded(supplierId, price, responseTime, receivedAt));

    /// <summary>Records a supplier's failure.</summary>
    /// <param name="supplierId">A selected supplier without a response yet.</param>
    /// <param name="errorCode">A machine-readable error code.</param>
    /// <param name="errorMessage">A human-readable error message.</param>
    /// <param name="responseTime">How long the supplier took to fail.</param>
    /// <param name="receivedAt">When the failure arrived (UTC).</param>
    /// <returns>The recorded response.</returns>
    public SupplierResponse RecordFailure(
        SupplierId supplierId,
        string errorCode,
        string errorMessage,
        TimeSpan responseTime,
        DateTime receivedAt) =>
        Add(SupplierResponse.Failed(supplierId, errorCode, errorMessage, responseTime, receivedAt));

    /// <summary>Ends the search as completed; every selected supplier must have responded.</summary>
    /// <param name="completedAt">When the search ended (UTC).</param>
    public void Complete(DateTime completedAt)
    {
        EnsureRunning();
        if (HasPendingSuppliers)
        {
            throw new InvalidOperationException("A search can't complete while suppliers are still pending.");
        }

        End(SearchStatus.Completed, completedAt);
    }

    /// <summary>Ends the search at its deadline, recording pending suppliers as timed out.</summary>
    /// <param name="at">The deadline (UTC).</param>
    /// <returns>The responses recorded for the pending suppliers.</returns>
    public IReadOnlyList<SupplierResponse> TimeOut(DateTime at) => Close(SearchStatus.TimedOut, ResponseOutcome.TimedOut, at);

    /// <summary>Ends the search because the client went away, recording pending suppliers as cancelled.</summary>
    /// <param name="at">When the search was cancelled (UTC).</param>
    /// <returns>The responses recorded for the pending suppliers.</returns>
    public IReadOnlyList<SupplierResponse> Cancel(DateTime at) => Close(SearchStatus.Cancelled, ResponseOutcome.Cancelled, at);

    /// <summary>Ends the search after an unexpected error, recording pending suppliers as cancelled.</summary>
    /// <param name="at">When the search faulted (UTC).</param>
    /// <returns>The responses recorded for the pending suppliers.</returns>
    public IReadOnlyList<SupplierResponse> Fault(DateTime at) => Close(SearchStatus.Faulted, ResponseOutcome.Cancelled, at);

    private List<SupplierResponse> Close(SearchStatus status, ResponseOutcome pendingOutcome, DateTime at)
    {
        EnsureRunning();
        TimeSpan waited = WaitedUntil(at);
        List<SupplierResponse> closing = [.. PendingSuppliers.Select(id => SupplierResponse.NoResponse(id, pendingOutcome, waited, at))];
        foreach (SupplierResponse response in closing)
        {
            Add(response);
        }

        End(status, at);
        return closing;
    }

    private SupplierResponse Add(SupplierResponse response)
    {
        EnsureRunning();
        if (!SelectedSuppliers.Contains(response.SupplierId))
        {
            throw new InvalidOperationException($"Supplier '{response.SupplierId}' wasn't selected for this search.");
        }

        if (!_answered.Add(response.SupplierId))
        {
            throw new InvalidOperationException($"Supplier '{response.SupplierId}' already has a response.");
        }

        _responses.Add(response);
        return response;
    }

    private void End(SearchStatus status, DateTime at)
    {
        WaitedUntil(at);
        Status = status;
        CompletedAt = at;
    }

    private TimeSpan WaitedUntil(DateTime at)
    {
        TimeSpan waited = UtcTimestamp.Require(at, nameof(at)) - CreatedAt;
        ArgumentOutOfRangeException.ThrowIfLessThan(waited, TimeSpan.Zero, nameof(at));
        return waited;
    }

    private void EnsureRunning()
    {
        if (Status != SearchStatus.Running)
        {
            throw new InvalidOperationException($"The search has already ended ({Status}).");
        }
    }
}
