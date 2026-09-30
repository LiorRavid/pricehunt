namespace PriceHunt.Application.Searches;

/// <summary>The raw search input, validated by <see cref="SearchPlanner"/>.</summary>
/// <param name="Origin">Where the goods ship from.</param>
/// <param name="Destination">Where the goods ship to.</param>
/// <param name="FromDate">The first shipping day.</param>
/// <param name="ToDate">The last shipping day.</param>
/// <param name="SupplierIds">The suppliers to query; <see langword="null"/> or empty means all.</param>
public sealed record SearchRequest(
    string? Origin,
    string? Destination,
    DateOnly? FromDate,
    DateOnly? ToDate,
    IReadOnlyList<string>? SupplierIds);
