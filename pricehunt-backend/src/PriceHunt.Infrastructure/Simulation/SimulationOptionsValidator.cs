using Microsoft.Extensions.Options;
using PriceHunt.Domain;

namespace PriceHunt.Infrastructure.Simulation;

/// <summary>Validates the simulation settings at startup, including every configured supplier.</summary>
internal sealed class SimulationOptionsValidator : IValidateOptions<SimulationOptions>
{
    public ValidateOptionsResult Validate(string? name, SimulationOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        List<string> failures = [];

        if (options.MinDelay < TimeSpan.Zero || options.MinDelay > options.MaxDelay)
        {
            failures.Add("Simulation:MinDelay must be zero or more and not above Simulation:MaxDelay.");
        }

        if (options.Suppliers.Count == 0)
        {
            failures.Add("Simulation:Suppliers must list at least one supplier.");
        }

        foreach (IGrouping<string, SimulatedSupplierOptions> duplicate in options.Suppliers.GroupBy(supplier => supplier.Id).Where(group => group.Count() > 1))
        {
            failures.Add($"Supplier id '{duplicate.Key}' is configured more than once.");
        }

        foreach (SimulatedSupplierOptions supplier in options.Suppliers)
        {
            failures.AddRange(ValidateSupplier(supplier));
        }

        return failures.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(failures);
    }

    private static IEnumerable<string> ValidateSupplier(SimulatedSupplierOptions supplier)
    {
        if (!SupplierId.TryCreate(supplier.Id, out _))
        {
            yield return $"Supplier id '{supplier.Id}' must be a lowercase slug such as 'albatross-freight'.";
        }

        if (string.IsNullOrWhiteSpace(supplier.Name))
        {
            yield return $"Supplier '{supplier.Id}' needs a name.";
        }

        if (supplier.Behavior != SupplierBehavior.Unresponsive && (supplier.MinPrice <= 0m || supplier.MinPrice > supplier.MaxPrice))
        {
            yield return $"Supplier '{supplier.Id}' needs a price range with 0 < MinPrice <= MaxPrice.";
        }

        if (supplier.Behavior == SupplierBehavior.Flaky && supplier.FailureRate is < 0 or > 1)
        {
            yield return $"Supplier '{supplier.Id}' needs a FailureRate between 0 and 1.";
        }
    }
}
