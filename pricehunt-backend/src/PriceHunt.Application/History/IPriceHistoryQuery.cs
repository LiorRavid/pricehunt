namespace PriceHunt.Application.History;

/// <summary>Reads the price history (the read side); filtering, sorting and paging run in the database.</summary>
public interface IPriceHistoryQuery
{
    /// <summary>Gets one page of supplier responses matching <paramref name="filter"/>.</summary>
    /// <param name="filter">The filters, sort order and page.</param>
    /// <param name="cancellationToken">A token to cancel the query.</param>
    /// <returns>The page and the total number of matches.</returns>
    Task<PagedResult<PriceHistoryItem>> GetAsync(PriceHistoryFilter filter, CancellationToken cancellationToken);
}
