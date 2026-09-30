namespace PriceHunt.Infrastructure.Simulation;

/// <summary>
/// A thread-safe <see cref="Random"/>: reproducible when seeded (for demos and end-to-end runs),
/// unpredictable when not.
/// </summary>
internal sealed class SeededRandomSource(int? seed) : IRandomSource
{
    private readonly Lock _gate = new();
    private readonly Random _random = seed is { } value ? new Random(value) : new Random();

    public double NextDouble()
    {
        lock (_gate)
        {
            return _random.NextDouble();
        }
    }

    public long NextInt64(long minValue, long maxValue)
    {
        lock (_gate)
        {
            return _random.NextInt64(minValue, maxValue);
        }
    }
}
