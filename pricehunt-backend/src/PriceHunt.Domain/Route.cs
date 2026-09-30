using System.Diagnostics.CodeAnalysis;

namespace PriceHunt.Domain;

/// <summary>An origin and a different destination.</summary>
public sealed record Route
{
    private Route(Location origin, Location destination)
    {
        Origin = origin;
        Destination = destination;
    }

    /// <summary>Gets where the goods ship from.</summary>
    public Location Origin { get; }

    /// <summary>Gets where the goods ship to.</summary>
    public Location Destination { get; }

    /// <summary>Creates a route, or throws when both ends are the same place.</summary>
    /// <param name="origin">Where the goods ship from.</param>
    /// <param name="destination">Where the goods ship to.</param>
    /// <returns>The route.</returns>
    /// <exception cref="ArgumentException">The destination equals the origin.</exception>
    public static Route Create(Location origin, Location destination) =>
        TryCreate(origin, destination, out Route? route, out string? error)
            ? route
            : throw new ArgumentException(error, nameof(destination));

    /// <summary>Tries to create a route; the ends are compared case-insensitively.</summary>
    /// <param name="origin">Where the goods ship from.</param>
    /// <param name="destination">Where the goods ship to.</param>
    /// <param name="route">The route, when valid.</param>
    /// <param name="error">A user-facing reason, when invalid.</param>
    /// <returns><see langword="true"/> when the ends differ.</returns>
    public static bool TryCreate(
        Location origin,
        Location destination,
        [NotNullWhen(true)] out Route? route,
        [NotNullWhen(false)] out string? error)
    {
        ArgumentNullException.ThrowIfNull(origin);
        ArgumentNullException.ThrowIfNull(destination);

        if (origin.Normalized == destination.Normalized)
        {
            (route, error) = (null, "The destination must differ from the origin.");
            return false;
        }

        (route, error) = (new Route(origin, destination), null);
        return true;
    }
}
