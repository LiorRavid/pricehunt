using PriceHunt.Domain;

namespace PriceHunt.Infrastructure.Simulation;

/// <summary>Draws a supplier's response delay, price and failure decision from one random source.</summary>
internal sealed class QuoteGenerator(
    IRandomSource random,
    TimeSpan minDelay,
    TimeSpan maxDelay,
    decimal minPrice,
    decimal maxPrice,
    string currency = "USD")
{
    /// <summary>Draws a delay, uniformly in [min, max] to the millisecond.</summary>
    public TimeSpan NextDelay() =>
        TimeSpan.FromMilliseconds(random.NextInt64((long)minDelay.TotalMilliseconds, (long)maxDelay.TotalMilliseconds + 1));

    /// <summary>Draws a price, uniformly in [min, max] to the cent.</summary>
    public Money NextPrice()
    {
        long cents = random.NextInt64(decimal.ToInt64(minPrice * 100m), decimal.ToInt64(maxPrice * 100m) + 1);
        return Money.FromMinorUnits(cents, currency);
    }

    /// <summary>Decides whether this call fails.</summary>
    public bool ShouldFail(double failureRate) => FailureDecision.ShouldFail(random, failureRate);
}
