using Microsoft.Extensions.Options;
using PriceHunt.Application.Suppliers;

namespace PriceHunt.Infrastructure.Simulation;

/// <summary>
/// The configured simulated suppliers, in configuration order. Each has its own random source,
/// derived from <c>Simulation:Seed</c> when one is set so every supplier is reproducible.
/// </summary>
internal sealed class SimulatedSupplierCatalog(IOptions<SimulationOptions> options, TimeProvider timeProvider) : ISupplierCatalog
{
    public IReadOnlyList<IShippingSupplier> Suppliers { get; } =
        [.. options.Value.Suppliers.Select((supplier, index) => Create(supplier, index, options.Value, timeProvider))];

    private static IShippingSupplier Create(SimulatedSupplierOptions supplier, int index, SimulationOptions simulation, TimeProvider timeProvider)
    {
        int? seed = simulation.Seed is { } value ? unchecked((value * 397) ^ index) : null;
        var quotes = new QuoteGenerator(new SeededRandomSource(seed), simulation.MinDelay, simulation.MaxDelay, supplier.MinPrice, supplier.MaxPrice);

        return supplier.Behavior switch
        {
            SupplierBehavior.Flaky => new FlakySupplier(supplier, quotes, timeProvider),
            SupplierBehavior.Unresponsive => new UnresponsiveSupplier(supplier, timeProvider),
            _ => new ReliableSupplier(supplier, quotes, timeProvider),
        };
    }
}
