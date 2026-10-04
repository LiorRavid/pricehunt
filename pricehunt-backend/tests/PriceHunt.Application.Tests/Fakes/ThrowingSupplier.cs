using PriceHunt.Application.Suppliers;
using PriceHunt.Domain;

namespace PriceHunt.Application.Tests.Fakes;

/// <summary>
/// A badly behaved supplier that throws synchronously instead of returning a faulted task. It builds
/// the exception when it's called, as a real supplier would.
/// </summary>
internal sealed class ThrowingSupplier(string id, Func<Exception> createException) : IShippingSupplier
{
    public SupplierId Id { get; } = SupplierId.Create(id);

    public string DisplayName { get; } = $"Supplier {id}";

    public Task<Money> GetQuoteAsync(SearchCriteria criteria, CancellationToken cancellationToken) => throw createException();
}
