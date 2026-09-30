using System.Net.ServerSentEvents;
using Microsoft.AspNetCore.Http.HttpResults;
using PriceHunt.Application.Searches;

namespace PriceHunt.Api.Searches;

/// <summary>The streaming search endpoint (ADR-001).</summary>
internal static class SearchEndpoints
{
    public static IEndpointRouteBuilder MapSearchEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/searches", StartSearch)
            .WithName("StartSearch")
            .WithTags("Searches")
            .WithSummary("Runs a search and streams each supplier outcome as a server-sent event.")
            .WithDescription("Events: search-started, then quote-received and supplier-failed in completion order, then exactly one search-completed. A search ends after 6 seconds at the latest; closing the connection cancels it.")
            .Produces(StatusCodes.Status200OK, contentType: "text/event-stream")
            .ProducesValidationProblem();
        return app;
    }

    private static Results<ServerSentEventsResult<SearchStreamEvent>, ValidationProblem> StartSearch(
        StartSearchRequest request,
        SearchPlanner planner,
        SearchOrchestrator orchestrator,
        HttpContext httpContext)
    {
        // Validate before streaming: once the stream starts, the status code can't change.
        SearchPlanResult result = planner.Plan(request.ToSearchRequest());
        if (!result.IsValid)
        {
            return TypedResults.ValidationProblem(result.Errors.ToDictionary());
        }

        // The request's abort token reaches every supplier call (SV5).
        IAsyncEnumerable<SearchEvent> events = orchestrator.RunAsync(result.Plan, httpContext.RequestAborted);
        IAsyncEnumerable<SseItem<SearchStreamEvent>> stream = SearchEventStream.ToServerSentEventsAsync(events, httpContext.RequestAborted);
        return TypedResults.ServerSentEvents(stream);
    }
}
