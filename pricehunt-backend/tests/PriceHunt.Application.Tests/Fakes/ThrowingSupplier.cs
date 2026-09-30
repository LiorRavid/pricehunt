using PriceHunt.Application.Suppliers;
using PriceHunt.Domain;

namespace PriceHunt.Application.Tests.Fakes;

/// <summary>A badly behaved supplier that throws synchronously instead of returning a faulted task.</summary>
internal sealed class ThrowingSupplier(string id, Exception exception) : IShippingSupplier
{
    public SupplierId Id { get; } = SupplierId.Create(id);

    public string DisplayName { get; } = $"Supplier {id}";

    public Task<Money> GetQuoteAsync(SearchCriteria criteria, CancellationToken cancellationToken) => throw exception;
}
