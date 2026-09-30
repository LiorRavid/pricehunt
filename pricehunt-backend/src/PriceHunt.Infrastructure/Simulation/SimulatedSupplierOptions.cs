namespace PriceHunt.Infrastructure.Simulation;

/// <summary>One simulated supplier, as configured under <c>Simulation:Suppliers</c>.</summary>
internal sealed record SimulatedSupplierOptions
{
    public string Id { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public SupplierBehavior Behavior { get; set; } = SupplierBehavior.Reliable;

    public decimal MinPrice { get; set; }

    public decimal MaxPrice { get; set; }

    /// <summary>Gets or sets the probability of failing; used by <see cref="SupplierBehavior.Flaky"/> only.</summary>
    public double FailureRate { get; set; }
}
