namespace PriceHunt.Infrastructure.Simulation;

/// <summary>How a simulated supplier behaves.</summary>
internal enum SupplierBehavior
{
    /// <summary>Always answers with a price after a random delay.</summary>
    Reliable,

    /// <summary>Answers after a random delay, but fails with a configured probability.</summary>
    Flaky,

    /// <summary>Never answers; only cancellation ends the call.</summary>
    Unresponsive,
}
