namespace PriceHunt.Api.History;

/// <summary>One page of the price history, with the total number of matches.</summary>
internal sealed record HistoryPageDto(IReadOnlyList<HistoryItemDto> Items, int Page, int PageSize, int TotalCount);
