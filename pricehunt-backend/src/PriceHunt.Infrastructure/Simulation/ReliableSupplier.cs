using PriceHunt.Application.Suppliers;
using PriceHunt.Domain;

namespace PriceHunt.Infrastructure.Simulation;

/// <summary>A simulated supplier that always answers with a price after a random delay.</summary>
internal sealed class ReliableSupplier(SimulatedSupplierOptions options, QuoteGenerator quotes, TimeProvider timeProvider)
    : IShippingSupplier
{
    public SupplierId Id { get; } = SupplierId.Create(options.Id);

    public string DisplayName { get; } = options.Name;

    public async Task<Money> GetQuoteAsync(SearchCriteria criteria, CancellationToken cancellationToken)
    {
        await Task.Delay(quotes.NextDelay(), timeProvider, cancellationToken).ConfigureAwait(false);
        return quotes.NextPrice();
    }
}
