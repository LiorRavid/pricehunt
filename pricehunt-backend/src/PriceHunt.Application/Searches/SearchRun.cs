using Microsoft.Extensions.Logging;
using PriceHunt.Application.Suppliers;
using PriceHunt.Domain;

namespace PriceHunt.Application.Searches;

/// <summary>
/// One running search (ADR-002). It starts every supplier call at once, turns their outcomes into
/// events in completion order, and ends the search exactly once. All error handling lives here, in
/// ordinary async methods, which keeps <see cref="SearchOrchestrator"/>'s iterator free of
/// try/catch (C# can't yield inside one).
/// </summary>
internal sealed class SearchRun : IAsyncDisposable
{
    private const string UnexpectedErrorCode = "unexpected_error";
    private const string UnexpectedErrorMessage = "The supplier returned an unexpected error.";

    private readonly Search _search;
    private readonly ISearchRepository _repository;
    private readonly TimeProvider _timeProvider;
    private readonly SearchOptions _options;
    private readonly ILogger _logger;
    private readonly CancellationToken _callerToken;
    private readonly CancellationTokenSource _deadline;
    private readonly CancellationTokenSource _calls;
    private readonly IAsyncEnumerator<Task<SupplierCallResult>> _completions;
    private bool _abandoned;
    private bool _finished;

    private SearchRun(
        Search search,
        SearchPlan plan,
        ISearchRepository repository,
        TimeProvider timeProvider,
        SearchOptions options,
        ILogger logger,
        CancellationToken callerToken)
    {
        _search = search;
        _repository = repository;
        _timeProvider = timeProvider;
        _options = options;
        _logger = logger;
        _callerToken = callerToken;

        // The budget runs from the moment the search started, whatever the initial write cost.
        DateTime deadline = search.CreatedAt + options.MaxDuration;
        TimeSpan remaining = deadline - UtcNow();
        _deadline = new CancellationTokenSource(remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero, timeProvider);
        _calls = CancellationTokenSource.CreateLinkedTokenSource(callerToken, _deadline.Token);

        List<Task<SupplierCallResult>> calls = [.. plan.Suppliers.Select(supplier => CallAsync(supplier, plan.Criteria, _calls.Token))];

        // Not the caller's token: after a cancellation every call still settles promptly (as
        // interrupted), and draining them is how the search records who never answered.
        _completions = Task.WhenEach(calls).GetAsyncEnumerator(CancellationToken.None);

        Started = new SearchStarted(
            search.Id,
            [.. plan.Suppliers.Select(supplier => new SupplierSummary(supplier.Id, supplier.DisplayName))],
            search.CreatedAt,
            deadline,
            options.MaxDuration);
    }

    /// <summary>Gets the first event of the search.</summary>
    public SearchStarted Started { get; }

    private bool IsCallerGone => _abandoned || _callerToken.IsCancellationRequested;

    /// <summary>Persists a new running search, arms its deadline and starts every supplier call.</summary>
    public static async Task<SearchRun> StartAsync(
        SearchPlan plan,
        ISearchRepository repository,
        TimeProvider timeProvider,
        SearchOptions options,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        var search = Search.Start(plan.Criteria, plan.Suppliers.Select(supplier => supplier.Id), timeProvider.GetUtcNow().UtcDateTime);
        using IDisposable? scope = SearchLog.BeginSearchScope(logger, search.Id);
        await repository.AddAsync(search, cancellationToken).ConfigureAwait(false);
        var run = new SearchRun(search, plan, repository, timeProvider, options, logger, cancellationToken);
        SearchLog.SearchStarted(logger, plan.Suppliers.Count, run.Started.Deadline);
        return run;
    }

    /// <summary>
    /// Waits for the next supplier outcome and records it. Returns the next event, or
    /// <see langword="null"/> once the search has ended (nothing follows the terminal event, and a
    /// cancelled search emits nothing more).
    /// </summary>
    public async ValueTask<SearchEvent?> NextAsync()
    {
        if (_finished)
        {
            return null;
        }

        using IDisposable? scope = SearchLog.BeginSearchScope(_logger, _search.Id);
        try
        {
            while (await _completions.MoveNextAsync().ConfigureAwait(false))
            {
                SupplierCallResult result = await _completions.Current.ConfigureAwait(false);
                SearchEvent? searchEvent = await RecordAsync(result).ConfigureAwait(false);
                if (searchEvent is not null && !IsCallerGone)
                {
                    return searchEvent;
                }
            }

            return await FinishAsync().ConfigureAwait(false);
        }
        catch (Exception exception) when (!exception.IsCritical())
        {
            return await FaultAsync(exception).ConfigureAwait(false);
        }
    }

    /// <summary>Cancels outstanding calls and, if the consumer stopped early, ends the search as cancelled.</summary>
    public async ValueTask DisposeAsync()
    {
        if (!_finished)
        {
            _abandoned = true;
            await _calls.CancelAsync().ConfigureAwait(false);
            await NextAsync().ConfigureAwait(false);
        }

        await _completions.DisposeAsync().ConfigureAwait(false);
        _calls.Dispose();
        _deadline.Dispose();
    }

    private async Task<SupplierCallResult> CallAsync(IShippingSupplier supplier, SearchCriteria criteria, CancellationToken cancellationToken)
    {
        long startedAt = _timeProvider.GetTimestamp();
        Task<Money>? call = null;
        try
        {
            call = supplier.GetQuoteAsync(criteria, cancellationToken);

            // WaitAsync keeps the deadline even when a supplier ignores its token.
            Money price = await call.WaitAsync(cancellationToken).ConfigureAwait(false);
            return new QuotedCall(supplier, price, _timeProvider.GetElapsedTime(startedAt), UtcNow());
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            ObserveAbandonedCall(call);
            return new InterruptedCall(supplier, _timeProvider.GetElapsedTime(startedAt), UtcNow());
        }
        catch (SupplierException exception)
        {
            return new FailedCall(supplier, exception.ErrorCode, exception.Message, _timeProvider.GetElapsedTime(startedAt), UtcNow());
        }
        catch (Exception exception) when (!exception.IsCritical())
        {
            // The supplier boundary is a fault barrier (SV4): one supplier's bug never reaches the others.
            SearchLog.SupplierThrewUnexpectedly(_logger, supplier.Id.Value, exception);
            return new FailedCall(supplier, UnexpectedErrorCode, UnexpectedErrorMessage, _timeProvider.GetElapsedTime(startedAt), UtcNow());
        }
    }

    private async Task<SearchEvent?> RecordAsync(SupplierCallResult result)
    {
        switch (result)
        {
            case QuotedCall quoted:
                SupplierResponse quote = _search.RecordQuote(quoted.Supplier.Id, quoted.Price, quoted.Elapsed, quoted.EndedAt);
                await PersistAsync(token => _repository.AddResponseAsync(_search.Id, quote, token)).ConfigureAwait(false);
                SearchLog.QuoteReceived(_logger, quote.SupplierId.Value, quoted.Price.Amount, quoted.Price.Currency, (long)quoted.Elapsed.TotalMilliseconds);
                return new QuoteReceived(_search.Id, quote.SupplierId, quoted.Price, quote.ResponseTime, quote.ReceivedAt);

            case FailedCall failed:
                SupplierResponse failure = _search.RecordFailure(failed.Supplier.Id, failed.ErrorCode, failed.ErrorMessage, failed.Elapsed, failed.EndedAt);
                await PersistAsync(token => _repository.AddResponseAsync(_search.Id, failure, token)).ConfigureAwait(false);
                SearchLog.SupplierFailed(_logger, failure.SupplierId.Value, failed.ErrorCode, (long)failed.Elapsed.TotalMilliseconds);
                return new SupplierFailed(_search.Id, failure.SupplierId, failed.ErrorCode, failed.ErrorMessage, failure.ResponseTime, failure.ReceivedAt);

            default:
                // Interrupted by the deadline or the caller: closed when the search ends.
                return null;
        }
    }

    private async Task<SearchEvent?> FinishAsync()
    {
        DateTime now = UtcNow();
        bool callerGone = IsCallerGone;
        IReadOnlyList<SupplierResponse> closing;
        if (!_search.HasPendingSuppliers)
        {
            _search.Complete(now);
            closing = [];
            SearchLog.SearchCompleted(_logger);
        }
        else if (callerGone)
        {
            closing = _search.Cancel(now);
            SearchLog.SearchCancelled(_logger, closing.Count);
        }
        else
        {
            closing = _search.TimeOut(now);
            SearchLog.SearchTimedOut(_logger, closing.Count);
        }

        await PersistAsync(token => _repository.SaveOutcomeAsync(_search, closing, token)).ConfigureAwait(false);
        _finished = true;
        return callerGone ? null : Completed(_search.Status, now);
    }

    private async Task<SearchEvent?> FaultAsync(Exception exception)
    {
        SearchLog.SearchFaulted(_logger, exception);
        _finished = true;
        await _calls.CancelAsync().ConfigureAwait(false);

        DateTime now = UtcNow();
        if (_search.Status == SearchStatus.Running)
        {
            IReadOnlyList<SupplierResponse> closing = _search.Fault(now);
            try
            {
                await PersistAsync(token => _repository.SaveOutcomeAsync(_search, closing, token)).ConfigureAwait(false);
            }
            catch (Exception persistenceFailure) when (!persistenceFailure.IsCritical())
            {
                // The row stays Running; the next startup closes it as cancelled.
                SearchLog.FinalisationFailed(_logger, persistenceFailure);
            }
        }

        return IsCallerGone ? null : Completed(SearchStatus.Faulted, now);
    }

    private SearchCompleted Completed(SearchStatus status, DateTime completedAt)
    {
        HashSet<SupplierId> responded = [.. _search.Responses
            .Where(response => response.Outcome is ResponseOutcome.Succeeded or ResponseOutcome.Failed)
            .Select(response => response.SupplierId)];

        return new SearchCompleted(
            _search.Id,
            status,
            completedAt,
            _search.Responses.Count(response => response.Outcome == ResponseOutcome.Succeeded),
            _search.Responses.Count(response => response.Outcome == ResponseOutcome.Failed),
            [.. _search.SelectedSuppliers.Where(id => !responded.Contains(id))]);
    }

    private async Task PersistAsync(Func<CancellationToken, Task> write)
    {
        // Never the caller's token: an arrived response and the final state are recorded even after
        // the client has gone. A short timeout bounds each write instead.
        using var timeout = new CancellationTokenSource(_options.PersistenceTimeout, _timeProvider);
        await write(timeout.Token).ConfigureAwait(false);
    }

    private DateTime UtcNow() => _timeProvider.GetUtcNow().UtcDateTime;

    private static void ObserveAbandonedCall(Task? call)
    {
        // A supplier that ignored its token keeps running; observe a late failure so it isn't
        // reported as an unobserved task exception.
        call?.ContinueWith(
            static task => _ = task.Exception,
            CancellationToken.None,
            TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously,
            TaskScheduler.Default);
    }
}
