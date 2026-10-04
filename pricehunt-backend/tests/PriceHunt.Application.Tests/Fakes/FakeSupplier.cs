using PriceHunt.Application.Suppliers;
using PriceHunt.Domain;

namespace PriceHunt.Application.Tests.Fakes;

/// <summary>
/// A supplier whose answer the test controls through <see cref="Respond"/> and <see cref="Fail"/>.
/// It signals when it is called and when it observes cancellation, so tests await signals
/// instead of sleeping.
/// </summary>
internal sealed class FakeSupplier(string id, bool ignoresCancellation = false) : IShippingSupplier
{
    private readonly TaskCompletionSource<Money> _answer = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly TaskCompletionSource _called = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly TaskCompletionSource _cancellationObserved = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private int _callCount;

    public SupplierId Id { get; } = SupplierId.Create(id);

    public string DisplayName { get; } = $"Supplier {id}";

    public int CallCount => Volatile.Read(ref _callCount);

    public SearchCriteria? LastCriteria { get; private set; }

    public Task Called => _called.Task;

    public Task CancellationObserved => _cancellationObserved.Task;

    public Task<Money> GetQuoteAsync(SearchCriteria criteria, CancellationToken cancellationToken)
    {
        Interlocked.Increment(ref _callCount);
        LastCriteria = criteria;
        if (!ignoresCancellation)
        {
            _ = cancellationToken.Register(() =>
            {
                _cancellationObserved.TrySetResult();
                _answer.TrySetCanceled(cancellationToken);
            });
        }

        _called.TrySetResult();
        return _answer.Task;
    }

    // A late answer after cancellation is ignored, like a real reply nobody is waiting for any more.
    public void Respond(decimal amount) => _answer.TrySetResult(Money.Create(amount, "USD"));

    // A supplier breaking the port's contract: nullable annotations aren't enforced at run time.
    public void RespondWithoutPrice() => _answer.TrySetResult(null!);

    public void Fail(Exception exception) => _answer.TrySetException(exception);
}
