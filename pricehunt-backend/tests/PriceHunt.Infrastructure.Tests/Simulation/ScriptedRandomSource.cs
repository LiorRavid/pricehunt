using PriceHunt.Infrastructure.Simulation;

namespace PriceHunt.Infrastructure.Tests.Simulation;

/// <summary>A random source that returns scripted values, for exact expectations.</summary>
internal sealed class ScriptedRandomSource(double sample) : IRandomSource
{
    public double NextDouble() => sample;

    public long NextInt64(long minValue, long maxValue) => minValue + (long)Math.Floor(sample * (maxValue - minValue));
}
