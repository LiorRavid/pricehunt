namespace PriceHunt.Infrastructure.Simulation;

/// <summary>The simulated suppliers, bound from the <c>Simulation</c> section and validated at startup.</summary>
internal sealed class SimulationOptions
{
    public const string SectionName = "Simulation";

    /// <summary>Gets or sets the seed for reproducible runs (<c>Simulation__Seed</c>); <see langword="null"/> means unpredictable.</summary>
    public int? Seed { get; set; }

    public TimeSpan MinDelay { get; set; } = TimeSpan.FromMilliseconds(500);

    public TimeSpan MaxDelay { get; set; } = TimeSpan.FromSeconds(5);

    public List<SimulatedSupplierOptions> Suppliers { get; set; } = [];
}
