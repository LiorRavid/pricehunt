namespace PriceHunt.Infrastructure.Simulation;

/// <summary>Whether a flaky supplier fails this time; pure, so its rate can be tested directly.</summary>
internal static class FailureDecision
{
    public static bool ShouldFail(IRandomSource random, double failureRate) => random.NextDouble() < failureRate;
}
