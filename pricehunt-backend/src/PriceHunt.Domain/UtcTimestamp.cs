namespace PriceHunt.Domain;

/// <summary>Guards that timestamps entering the domain are UTC.</summary>
internal static class UtcTimestamp
{
    public static DateTime Require(DateTime value, string parameterName) =>
        value.Kind == DateTimeKind.Utc
            ? value
            : throw new ArgumentException("Timestamps must be UTC (DateTimeKind.Utc).", parameterName);
}
