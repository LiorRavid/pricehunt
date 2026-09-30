using System.Text.RegularExpressions;

namespace PriceHunt.Domain;

/// <summary>
/// A non-negative amount in an ISO-4217 currency with at most two decimals, so it maps exactly
/// to integer minor units (cents).
/// </summary>
public sealed partial record Money
{
    private Money(decimal amount, string currency)
    {
        Amount = amount;
        Currency = currency;
    }

    /// <summary>Gets the amount in major units, for example dollars.</summary>
    public decimal Amount { get; }

    /// <summary>Gets the ISO-4217 currency code, for example <c>USD</c>.</summary>
    public string Currency { get; }

    /// <summary>Creates an amount of money.</summary>
    /// <param name="amount">The amount in major units, at most two decimals.</param>
    /// <param name="currency">The ISO-4217 currency code.</param>
    /// <returns>The money value.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The amount is negative.</exception>
    /// <exception cref="ArgumentException">The amount has more than two decimals, or the currency is not a code.</exception>
    public static Money Create(decimal amount, string currency)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(amount);
        if (decimal.Round(amount, 2) != amount)
        {
            throw new ArgumentException("An amount can have at most two decimals.", nameof(amount));
        }

        if (currency is null || !CurrencyCodePattern().IsMatch(currency))
        {
            throw new ArgumentException("The currency must be a three-letter ISO-4217 code, such as USD.", nameof(currency));
        }

        return new Money(amount, currency);
    }

    /// <summary>Creates an amount of money from integer minor units (cents).</summary>
    /// <param name="minorUnits">The amount in minor units.</param>
    /// <param name="currency">The ISO-4217 currency code.</param>
    /// <returns>The money value.</returns>
    public static Money FromMinorUnits(long minorUnits, string currency) => Create(minorUnits / 100m, currency);

    /// <summary>Gets the amount in integer minor units (cents).</summary>
    /// <returns>The amount multiplied by 100.</returns>
    public long ToMinorUnits() => decimal.ToInt64(Amount * 100m);

    [GeneratedRegex("^[A-Z]{3}$")]
    private static partial Regex CurrencyCodePattern();
}
