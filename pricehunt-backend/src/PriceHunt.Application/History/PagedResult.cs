namespace PriceHunt.Application.History;

/// <summary>One page of results.</summary>
/// <typeparam name="T">The item type.</typeparam>
/// <param name="Items">The items on this page.</param>
/// <param name="Page">The 1-based page number.</param>
/// <param name="PageSize">The requested page size.</param>
/// <param name="TotalCount">The number of matching items across all pages.</param>
public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount);
