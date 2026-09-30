using System.Diagnostics;
using PriceHunt.Application.Suppliers;
using PriceHunt.Domain;

namespace PriceHunt.Infrastructure.Simulation;

/// <summary>
/// A simulated supplier that never answers. It waits on an infinite delay that still honours
/// cancellation, so an abandoned call leaks nothing.
/// </summary>
internal sealed class UnresponsiveSupplier(SimulatedSupplierOptions options, TimeProvider timeProvider) : IShippingSupplier
{
    public SupplierId Id { get; } = SupplierId.Create(options.Id);

    public string DisplayName { get; } = options.Name;

    public async Task<Money> GetQuoteAsync(SearchCriteria criteria, CancellationToken cancellationToken)
    {
        await Task.Delay(Timeout.InfiniteTimeSpan, timeProvider, cancellationToken).ConfigureAwait(false);
        throw new UnreachableException("An infinite delay only ends through cancellation.");
    }
}
