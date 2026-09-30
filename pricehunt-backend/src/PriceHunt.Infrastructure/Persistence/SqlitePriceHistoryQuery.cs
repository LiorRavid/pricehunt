using Microsoft.EntityFrameworkCore;
using PriceHunt.Application.History;
using PriceHunt.Domain;

namespace PriceHunt.Infrastructure.Persistence;

/// <summary>
/// Filters, sorts and pages the price history in SQLite. Sorting is whitelisted, and a deterministic
/// tie-breaker (the time-ordered id) keeps pages stable.
/// </summary>
internal sealed class SqlitePriceHistoryQuery(IDbContextFactory<PriceHuntDbContext> contextFactory) : IPriceHistoryQuery
{
    public async Task<PagedResult<PriceHistoryItem>> GetAsync(PriceHistoryFilter filter, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(filter);
        ArgumentOutOfRangeException.ThrowIfLessThan(filter.Page, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(filter.PageSize, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(filter.PageSize, PriceHistoryFilter.MaxPageSize);

        await using PriceHuntDbContext db = await contextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        IQueryable<SupplierResponseEntity> responses = Filter(db.SupplierResponses.AsNoTracking(), filter);

        int totalCount = await responses.CountAsync(cancellationToken).ConfigureAwait(false);
        var rows = await Sort(responses, filter.SortBy, filter.SortDirection)
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .Select(response => new
            {
                response.Id,
                response.SearchId,
                response.ReceivedAt,
                response.Search!.Origin,
                response.Search.Destination,
                response.Search.ShipDateFrom,
                response.Search.ShipDateTo,
                response.SupplierId,
                SupplierName = response.Supplier!.Name,
                response.Outcome,
                response.PriceMinorUnits,
                response.Currency,
                response.ResponseTimeMs,
                response.ErrorCode,
            })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        PriceHistoryItem[] items =
        [
            .. rows.Select(row => new PriceHistoryItem(
                row.Id,
                row.SearchId,
                row.ReceivedAt,
                row.Origin,
                row.Destination,
                row.ShipDateFrom,
                row.ShipDateTo,
                row.SupplierId,
                row.SupplierName,
                row.Outcome,
                row is { PriceMinorUnits: { } minorUnits, Currency: { } currency } ? Money.FromMinorUnits(minorUnits, currency) : null,
                TimeSpan.FromMilliseconds(row.ResponseTimeMs),
                row.ErrorCode)),
        ];

        return new PagedResult<PriceHistoryItem>(items, filter.Page, filter.PageSize, totalCount);
    }

    private static IQueryable<SupplierResponseEntity> Filter(IQueryable<SupplierResponseEntity> responses, PriceHistoryFilter filter)
    {
        if (!filter.IncludeFailures)
        {
            responses = responses.Where(response => response.Outcome == ResponseOutcome.Succeeded);
        }

        if (filter.From is { } from)
        {
            responses = responses.Where(response => response.ReceivedAt >= from);
        }

        if (filter.To is { } to)
        {
            responses = responses.Where(response => response.ReceivedAt < to);
        }

        if (filter.Suppliers.Count > 0)
        {
            string[] supplierIds = [.. filter.Suppliers.Select(id => id.Value)];
            responses = responses.Where(response => supplierIds.Contains(response.SupplierId));
        }

        if (Normalize(filter.Origin) is { } origin)
        {
            responses = responses.Where(response => response.Search!.OriginNormalized.Contains(origin));
        }

        if (Normalize(filter.Destination) is { } destination)
        {
            responses = responses.Where(response => response.Search!.DestinationNormalized.Contains(destination));
        }

        return responses;
    }

    private static IOrderedQueryable<SupplierResponseEntity> Sort(
        IQueryable<SupplierResponseEntity> responses,
        HistorySortField sortBy,
        SortDirection direction)
    {
        bool ascending = direction == SortDirection.Ascending;
        IOrderedQueryable<SupplierResponseEntity> ordered = sortBy switch
        {
            HistorySortField.Date => ascending
                ? responses.OrderBy(response => response.ReceivedAt)
                : responses.OrderByDescending(response => response.ReceivedAt),
            HistorySortField.Route => ascending
                ? responses.OrderBy(response => response.Search!.OriginNormalized).ThenBy(response => response.Search!.DestinationNormalized)
                : responses.OrderByDescending(response => response.Search!.OriginNormalized).ThenByDescending(response => response.Search!.DestinationNormalized),
            HistorySortField.Supplier => ascending
                ? responses.OrderBy(response => response.Supplier!.Name)
                : responses.OrderByDescending(response => response.Supplier!.Name),

            // Responses without a price (failures) sort last in both directions.
            HistorySortField.Price => ascending
                ? responses.OrderBy(response => response.PriceMinorUnits == null).ThenBy(response => response.PriceMinorUnits)
                : responses.OrderBy(response => response.PriceMinorUnits == null).ThenByDescending(response => response.PriceMinorUnits),
            HistorySortField.ResponseTime => ascending
                ? responses.OrderBy(response => response.ResponseTimeMs)
                : responses.OrderByDescending(response => response.ResponseTimeMs),
            _ => throw new ArgumentOutOfRangeException(nameof(sortBy), sortBy, "Unsupported sort column."),
        };

        return ascending ? ordered.ThenBy(response => response.Id) : ordered.ThenByDescending(response => response.Id);
    }

    private static string? Normalize(string? text) =>
        string.IsNullOrWhiteSpace(text) ? null : text.Trim().ToUpperInvariant();
}
