namespace PriceHunt.Infrastructure.Simulation;

/// <summary>An injectable source of randomness, so simulations can be seeded and tests can script draws.</summary>
internal interface IRandomSource
{
    /// <summary>Returns a number in [0, 1).</summary>
    double NextDouble();

    /// <summary>Returns a whole number in [<paramref name="minValue"/>, <paramref name="maxValue"/>).</summary>
    long NextInt64(long minValue, long maxValue);
}
