using Microsoft.AspNetCore.Http.HttpResults;
using PriceHunt.Application.History;

namespace PriceHunt.Api.History;

/// <summary>The price history; the database does all filtering, sorting and paging.</summary>
internal static class HistoryEndpoints
{
    public static IEndpointRouteBuilder MapHistoryEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/history", GetHistoryAsync)
            .WithName("GetHistory")
            .WithTags("History")
            .WithSummary("Lists supplier quotes, filtered, sorted and paged on the server.")
            .WithDescription("startDate and endDate form a half-open interval [startDate, endDate) on the quote timestamp. Repeat suppliers to select several; omit it for all.")
            .ProducesValidationProblem();
        return app;
    }

    private static async Task<Results<Ok<HistoryPageDto>, ValidationProblem>> GetHistoryAsync(
        [AsParameters] HistoryQueryParameters parameters,
        IPriceHistoryQuery query,
        CancellationToken cancellationToken)
    {
        if (!parameters.TryCreateFilter(out PriceHistoryFilter? filter, out Dictionary<string, string[]> errors))
        {
            return TypedResults.ValidationProblem(errors);
        }

        PagedResult<PriceHistoryItem> page = await query.GetAsync(filter, cancellationToken);
        return TypedResults.Ok(new HistoryPageDto([.. page.Items.Select(HistoryItemDto.From)], page.Page, page.PageSize, page.TotalCount));
    }
}
