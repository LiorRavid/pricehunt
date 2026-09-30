using PriceHunt.Application.Searches;

namespace PriceHunt.Api.Searches;

/// <summary>
/// The body of <c>POST /api/searches</c>. Every rule (required fields, lengths, origin ≠
/// destination, date order, known suppliers) is checked once, by <see cref="SearchPlanner"/>, and
/// reported as a 400 problem with field errors before anything is streamed.
/// </summary>
internal sealed class StartSearchRequest
{
    /// <summary>Gets where the goods ship from.</summary>
    public string? Origin { get; init; }

    /// <summary>Gets where the goods ship to.</summary>
    public string? Destination { get; init; }

    /// <summary>Gets the first shipping day (<c>yyyy-MM-dd</c>).</summary>
    public DateOnly? FromDate { get; init; }

    /// <summary>Gets the last shipping day (<c>yyyy-MM-dd</c>).</summary>
    public DateOnly? ToDate { get; init; }

    /// <summary>Gets the suppliers to query; omitted or empty means all of them.</summary>
    public IReadOnlyList<string>? SupplierIds { get; init; }

    public SearchRequest ToSearchRequest() => new(Origin, Destination, FromDate, ToDate, SupplierIds);
}
