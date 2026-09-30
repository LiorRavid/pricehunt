using System.Globalization;
using System.Net.ServerSentEvents;
using System.Runtime.CompilerServices;
using PriceHunt.Api.Contracts;
using PriceHunt.Application.Searches;

namespace PriceHunt.Api.Searches;

/// <summary>Turns search events into server-sent events with a type and an increasing id.</summary>
internal static class SearchEventStream
{
    public const string SearchStartedType = "search-started";
    public const string QuoteReceivedType = "quote-received";
    public const string SupplierFailedType = "supplier-failed";
    public const string SearchCompletedType = "search-completed";

    public static async IAsyncEnumerable<SseItem<SearchStreamEvent>> ToServerSentEventsAsync(
        IAsyncEnumerable<SearchEvent> events,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        long sequence = 0;
        await foreach (SearchEvent searchEvent in events.WithCancellation(cancellationToken))
        {
            (string type, SearchStreamEvent payload) = Map(searchEvent);
            yield return new SseItem<SearchStreamEvent>(payload, type)
            {
                EventId = (++sequence).ToString(CultureInfo.InvariantCulture),
            };
        }
    }

    private static (string Type, SearchStreamEvent Payload) Map(SearchEvent searchEvent) => searchEvent switch
    {
        SearchStarted started => (SearchStartedType, new SearchStartedPayload(
            started.SearchId,
            [.. started.Suppliers.Select(supplier => new SupplierDto(supplier.Id.Value, supplier.Name))],
            started.StartedAt,
            started.Deadline,
            Milliseconds(started.MaxDuration))),
        QuoteReceived quote => (QuoteReceivedType, new QuoteReceivedPayload(
            quote.SearchId,
            quote.SupplierId.Value,
            MoneyDto.From(quote.Price),
            Milliseconds(quote.ResponseTime),
            quote.ReceivedAt)),
        SupplierFailed failure => (SupplierFailedType, new SupplierFailedPayload(
            failure.SearchId,
            failure.SupplierId.Value,
            failure.ErrorCode,
            failure.ErrorMessage,
            Milliseconds(failure.ResponseTime),
            failure.ReceivedAt)),
        SearchCompleted completed => (SearchCompletedType, new SearchCompletedPayload(
            completed.SearchId,
            completed.Status,
            completed.CompletedAt,
            completed.SucceededCount + completed.FailedCount,
            completed.SucceededCount,
            completed.FailedCount,
            [.. completed.NoResponseSupplierIds.Select(id => id.Value)])),
        _ => throw new ArgumentOutOfRangeException(nameof(searchEvent), searchEvent, "Unknown search event."),
    };

    private static long Milliseconds(TimeSpan duration) => (long)Math.Round(duration.TotalMilliseconds);
}
