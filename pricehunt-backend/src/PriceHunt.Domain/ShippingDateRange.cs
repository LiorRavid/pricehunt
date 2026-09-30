using System.Diagnostics.CodeAnalysis;

namespace PriceHunt.Domain;

/// <summary>The days the goods can ship, from and to inclusive.</summary>
public sealed record ShippingDateRange
{
    private ShippingDateRange(DateOnly from, DateOnly to)
    {
        From = from;
        To = to;
    }

    /// <summary>Gets the first shipping day.</summary>
    public DateOnly From { get; }

    /// <summary>Gets the last shipping day.</summary>
    public DateOnly To { get; }

    /// <summary>Creates a range, or throws when it ends before it starts.</summary>
    /// <param name="from">The first shipping day.</param>
    /// <param name="to">The last shipping day.</param>
    /// <returns>The range.</returns>
    /// <exception cref="ArgumentException"><paramref name="to"/> is before <paramref name="from"/>.</exception>
    public static ShippingDateRange Create(DateOnly from, DateOnly to) =>
        TryCreate(from, to, out ShippingDateRange? range, out string? error)
            ? range
            : throw new ArgumentException(error, nameof(to));

    /// <summary>Tries to create a range.</summary>
    /// <param name="from">The first shipping day.</param>
    /// <param name="to">The last shipping day.</param>
    /// <param name="range">The range, when valid.</param>
    /// <param name="error">A user-facing reason, when invalid.</param>
    /// <returns><see langword="true"/> when <paramref name="to"/> is not before <paramref name="from"/>.</returns>
    public static bool TryCreate(
        DateOnly from,
        DateOnly to,
        [NotNullWhen(true)] out ShippingDateRange? range,
        [NotNullWhen(false)] out string? error)
    {
        if (to < from)
        {
            (range, error) = (null, "The end date can't be before the start date.");
            return false;
        }

        (range, error) = (new ShippingDateRange(from, to), null);
        return true;
    }
}
