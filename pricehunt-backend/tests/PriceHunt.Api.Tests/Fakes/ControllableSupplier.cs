using PriceHunt.Application.Suppliers;
using PriceHunt.Domain;

namespace PriceHunt.Api.Tests.Fakes;

/// <summary>A supplier the test answers by hand; it signals when it sees cancellation.</summary>
internal sealed class ControllableSupplier(string id, string name) : IShippingSupplier
{
    private readonly TaskCompletionSource<Money> _answer = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly TaskCompletionSource _cancellationObserved = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public SupplierId Id { get; } = SupplierId.Create(id);

    public string DisplayName { get; } = name;

    public Task CancellationObserved => _cancellationObserved.Task;

    public Task<Money> GetQuoteAsync(SearchCriteria criteria, CancellationToken cancellationToken)
    {
        _ = cancellationToken.Register(() =>
        {
            _cancellationObserved.TrySetResult();
            _answer.TrySetCanceled(cancellationToken);
        });
        return _answer.Task;
    }

    public void Respond(decimal amount) => _answer.TrySetResult(Money.Create(amount, "USD"));

    public void Fail(Exception exception) => _answer.TrySetException(exception);
}
