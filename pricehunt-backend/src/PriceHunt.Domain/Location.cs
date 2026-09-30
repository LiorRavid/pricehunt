using System.Diagnostics.CodeAnalysis;

namespace PriceHunt.Domain;

/// <summary>A place goods ship from or to: trimmed, non-empty and length-limited.</summary>
public sealed record Location
{
    /// <summary>The maximum number of characters in a location.</summary>
    public const int MaxLength = 100;

    private Location(string value)
    {
        Value = value;
        Normalized = value.ToUpperInvariant();
    }

    /// <summary>Gets the location as entered, without surrounding whitespace.</summary>
    public string Value { get; }

    /// <summary>Gets the upper-invariant form used for case-insensitive matching and sorting.</summary>
    public string Normalized { get; }

    /// <summary>Creates a location, or throws when <paramref name="value"/> is invalid.</summary>
    /// <param name="value">The raw location text.</param>
    /// <returns>The location.</returns>
    /// <exception cref="ArgumentException">The value is missing or too long.</exception>
    public static Location Create(string? value) =>
        TryCreate(value, out Location? location, out string? error)
            ? location
            : throw new ArgumentException(error, nameof(value));

    /// <summary>Tries to create a location.</summary>
    /// <param name="value">The raw location text.</param>
    /// <param name="location">The location, when valid.</param>
    /// <param name="error">A user-facing reason, when invalid.</param>
    /// <returns><see langword="true"/> when the value is a valid location.</returns>
    public static bool TryCreate(
        string? value,
        [NotNullWhen(true)] out Location? location,
        [NotNullWhen(false)] out string? error)
    {
        string trimmed = value?.Trim() ?? string.Empty;
        if (trimmed.Length == 0)
        {
            (location, error) = (null, "A location is required.");
            return false;
        }

        if (trimmed.Length > MaxLength)
        {
            (location, error) = (null, $"A location can have at most {MaxLength} characters.");
            return false;
        }

        (location, error) = (new Location(trimmed), null);
        return true;
    }

    /// <inheritdoc />
    public override string ToString() => Value;
}
