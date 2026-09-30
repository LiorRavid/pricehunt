namespace PriceHunt.Application.Searches;

/// <summary>
/// Something that happened during a search, streamed to the client in order: one
/// <see cref="SearchStarted"/>, then <see cref="QuoteReceived"/> and <see cref="SupplierFailed"/>
/// in completion order, then exactly one <see cref="SearchCompleted"/>.
/// </summary>
/// <param name="SearchId">The search the event belongs to.</param>
public abstract record SearchEvent(Guid SearchId);
