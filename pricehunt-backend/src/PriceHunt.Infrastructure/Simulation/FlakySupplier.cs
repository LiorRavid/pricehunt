using PriceHunt.Application.Suppliers;
using PriceHunt.Domain;

namespace PriceHunt.Infrastructure.Simulation;

/// <summary>A simulated supplier that answers after a random delay but fails with a configured probability.</summary>
internal sealed class FlakySupplier(SimulatedSupplierOptions options, QuoteGenerator quotes, TimeProvider timeProvider)
    : IShippingSupplier
{
    public SupplierId Id { get; } = SupplierId.Create(options.Id);

    public string DisplayName { get; } = options.Name;

    public async Task<Money> GetQuoteAsync(SearchCriteria criteria, CancellationToken cancellationToken)
    {
        await Task.Delay(quotes.NextDelay(), timeProvider, cancellationToken).ConfigureAwait(false);
        return quotes.ShouldFail(options.FailureRate)
            ? throw new SupplierUnavailableException(DisplayName)
            : quotes.NextPrice();
    }
}
