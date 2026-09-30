using System.Diagnostics.CodeAnalysis;
using PriceHunt.Application.History;
using PriceHunt.Domain;
using HistorySortDirection = PriceHunt.Application.History.SortDirection;

namespace PriceHunt.Api.History;

/// <summary>
/// The query string of <c>GET /api/history</c>. Every rule lives in <see cref="TryCreateFilter"/>,
/// which reports invalid input as field errors (400).
/// </summary>
internal sealed class HistoryQueryParameters
{
    private static readonly Dictionary<string, HistorySortField> s_sortFields = new(StringComparer.OrdinalIgnoreCase)
    {
        ["date"] = HistorySortField.Date,
        ["route"] = HistorySortField.Route,
        ["supplier"] = HistorySortField.Supplier,
        ["price"] = HistorySortField.Price,
        ["responseTime"] = HistorySortField.ResponseTime,
    };

    private static readonly Dictionary<string, HistorySortDirection> s_sortDirections = new(StringComparer.OrdinalIgnoreCase)
    {
        ["asc"] = HistorySortDirection.Ascending,
        ["desc"] = HistorySortDirection.Descending,
    };

    /// <summary>Gets the earliest quote time to include (ISO-8601, inclusive).</summary>
    public DateTimeOffset? StartDate { get; init; }

    /// <summary>Gets the quote time to stop before (ISO-8601, exclusive).</summary>
    public DateTimeOffset? EndDate { get; init; }

    /// <summary>Gets the suppliers to include (repeat the parameter); none means all.</summary>
    public string[]? Suppliers { get; init; }

    /// <summary>Gets text the origin must contain, ignoring case.</summary>
    public string? Origin { get; init; }

    /// <summary>Gets text the destination must contain, ignoring case.</summary>
    public string? Destination { get; init; }

    /// <summary>Gets the sort column: date (default), route, supplier, price or responseTime.</summary>
    public string? SortBy { get; init; }

    /// <summary>Gets the sort direction: asc or desc (default).</summary>
    public string? SortDirection { get; init; }

    /// <summary>Gets the 1-based page number (default 1).</summary>
    public int? Page { get; init; }

    /// <summary>Gets the page size (default 20, at most 100).</summary>
    public int? PageSize { get; init; }

    /// <summary>Gets a value indicating whether failed, timed-out and cancelled responses are included (default false).</summary>
    public bool? IncludeFailures { get; init; }

    public bool TryCreateFilter([NotNullWhen(true)] out PriceHistoryFilter? filter, out Dictionary<string, string[]> errors)
    {
        errors = [];

        if (StartDate is { } start && EndDate is { } end && end <= start)
        {
            errors["endDate"] = ["The end date must be after the start date."];
        }

        string[] invalidSuppliers = [.. (Suppliers ?? []).Where(id => !SupplierId.TryCreate(id, out _))];
        if (invalidSuppliers.Length > 0)
        {
            errors["suppliers"] = [$"Invalid supplier ids: {string.Join(", ", invalidSuppliers.Select(id => $"'{id}'"))}."];
        }

        if (Origin is { Length: > Location.MaxLength })
        {
            errors["origin"] = [$"The origin filter can have at most {Location.MaxLength} characters."];
        }

        if (Destination is { Length: > Location.MaxLength })
        {
            errors["destination"] = [$"The destination filter can have at most {Location.MaxLength} characters."];
        }

        HistorySortField sortBy = HistorySortField.Date;
        if (SortBy is not null && !s_sortFields.TryGetValue(SortBy, out sortBy))
        {
            errors["sortBy"] = ["Sort by one of: date, route, supplier, price, responseTime."];
        }

        HistorySortDirection direction = HistorySortDirection.Descending;
        if (SortDirection is not null && !s_sortDirections.TryGetValue(SortDirection, out direction))
        {
            errors["sortDirection"] = ["The sort direction must be asc or desc."];
        }

        if (Page is < 1)
        {
            errors["page"] = ["The page must be 1 or more."];
        }

        if (PageSize is < 1 or > PriceHistoryFilter.MaxPageSize)
        {
            errors["pageSize"] = [$"The page size must be between 1 and {PriceHistoryFilter.MaxPageSize}."];
        }

        if (errors.Count > 0)
        {
            filter = null;
            return false;
        }

        filter = new PriceHistoryFilter
        {
            From = StartDate?.UtcDateTime,
            To = EndDate?.UtcDateTime,
            Suppliers = [.. (Suppliers ?? []).Distinct().Select(SupplierId.Create)],
            Origin = Origin,
            Destination = Destination,
            IncludeFailures = IncludeFailures ?? false,
            SortBy = sortBy,
            SortDirection = direction,
            Page = Page ?? 1,
            PageSize = PageSize ?? PriceHistoryFilter.DefaultPageSize,
        };
        return true;
    }
}
