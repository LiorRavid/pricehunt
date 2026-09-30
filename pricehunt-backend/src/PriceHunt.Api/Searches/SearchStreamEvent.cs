using System.Text.Json.Serialization;

namespace PriceHunt.Api.Searches;

/// <summary>
/// The JSON <c>data</c> of one server-sent event. The SSE <c>event</c> field names the type, so no
/// type discriminator is written; the derived-type attributes make System.Text.Json serialize the
/// derived properties rather than only the base type's.
/// </summary>
[JsonDerivedType(typeof(SearchStartedPayload))]
[JsonDerivedType(typeof(QuoteReceivedPayload))]
[JsonDerivedType(typeof(SupplierFailedPayload))]
[JsonDerivedType(typeof(SearchCompletedPayload))]
internal abstract record SearchStreamEvent(Guid SearchId);
