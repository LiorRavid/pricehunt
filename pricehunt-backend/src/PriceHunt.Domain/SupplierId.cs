using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

namespace PriceHunt.Domain;

/// <summary>A supplier's stable identifier: a lowercase slug such as <c>aurora-freightways</c>.</summary>
public sealed partial record SupplierId
{
    /// <summary>The maximum number of characters in a supplier id.</summary>
    public const int MaxLength = 64;

    private SupplierId(string value) => Value = value;

    /// <summary>Gets the slug.</summary>
    public string Value { get; }

    /// <summary>Creates a supplier id, or throws when <paramref name="value"/> is not a slug.</summary>
    /// <param name="value">The slug.</param>
    /// <returns>The supplier id.</returns>
    /// <exception cref="ArgumentException">The value is not a lowercase slug.</exception>
    public static SupplierId Create(string value) =>
        TryCreate(value, out SupplierId? id)
            ? id
            : throw new ArgumentException($"'{value}' is not a valid supplier id.", nameof(value));

    /// <summary>Tries to create a supplier id.</summary>
    /// <param name="value">The slug.</param>
    /// <param name="id">The supplier id, when valid.</param>
    /// <returns><see langword="true"/> when the value is a lowercase slug of at most <see cref="MaxLength"/> characters.</returns>
    public static bool TryCreate(string? value, [NotNullWhen(true)] out SupplierId? id)
    {
        id = value is { Length: > 0 and <= MaxLength } && SlugPattern().IsMatch(value)
            ? new SupplierId(value)
            : null;
        return id is not null;
    }

    /// <inheritdoc />
    public override string ToString() => Value;

    [GeneratedRegex("^[a-z0-9]+(-[a-z0-9]+)*$")]
    private static partial Regex SlugPattern();
}
