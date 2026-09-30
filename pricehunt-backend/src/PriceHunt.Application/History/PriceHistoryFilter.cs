using PriceHunt.Domain;

namespace PriceHunt.Application.History;

/// <summary>What to read from the price history, in what order, and which page.</summary>
public sealed record PriceHistoryFilter
{
    /// <summary>The page size when none is given.</summary>
    public const int DefaultPageSize = 20;

    /// <summary>The largest page size a client may ask for.</summary>
    public const int MaxPageSize = 100;

    /// <summary>Gets the earliest quote timestamp to include (UTC, inclusive).</summary>
    public DateTime? From { get; init; }

    /// <summary>Gets the quote timestamp to stop before (UTC, exclusive).</summary>
    public DateTime? To { get; init; }

    /// <summary>Gets the suppliers to include; empty means all.</summary>
    public IReadOnlyCollection<SupplierId> Suppliers { get; init; } = [];

    /// <summary>Gets text the origin must contain, ignoring case.</summary>
    public string? Origin { get; init; }

    /// <summary>Gets text the destination must contain, ignoring case.</summary>
    public string? Destination { get; init; }

    /// <summary>Gets a value indicating whether failed, timed-out and cancelled responses are included.</summary>
    public bool IncludeFailures { get; init; }

    /// <summary>Gets the column to sort by.</summary>
    public HistorySortField SortBy { get; init; } = HistorySortField.Date;

    /// <summary>Gets the sort direction.</summary>
    public SortDirection SortDirection { get; init; } = SortDirection.Descending;

    /// <summary>Gets the 1-based page number.</summary>
    public int Page { get; init; } = 1;

    /// <summary>Gets the number of items per page.</summary>
    public int PageSize { get; init; } = DefaultPageSize;
}
