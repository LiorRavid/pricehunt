using PriceHunt.Domain;

namespace PriceHunt.Infrastructure.Persistence;

/// <summary>Explicit mapping between the domain model and the persistence entities.</summary>
internal static class PersistenceMapping
{
    private const int ErrorMessageMaxLength = 500;

    public static SearchEntity ToEntity(Search search) => new()
    {
        Id = search.Id,
        Origin = search.Criteria.Route.Origin.Value,
        OriginNormalized = search.Criteria.Route.Origin.Normalized,
        Destination = search.Criteria.Route.Destination.Value,
        DestinationNormalized = search.Criteria.Route.Destination.Normalized,
        ShipDateFrom = search.Criteria.ShippingDates.From,
        ShipDateTo = search.Criteria.ShippingDates.To,
        CreatedAt = search.CreatedAt,
        CompletedAt = search.CompletedAt,
        Status = search.Status,
        Suppliers = [.. search.SelectedSuppliers.Select(id => new SearchSupplierEntity { SearchId = search.Id, SupplierId = id.Value })],
        Responses = [.. search.Responses.Select(response => ToEntity(search.Id, response))],
    };

    public static SupplierResponseEntity ToEntity(Guid searchId, SupplierResponse response) => new()
    {
        Id = response.Id,
        SearchId = searchId,
        SupplierId = response.SupplierId.Value,
        Outcome = response.Outcome,
        PriceMinorUnits = response.Price?.ToMinorUnits(),
        Currency = response.Price?.Currency,
        ResponseTimeMs = (int)Math.Round(response.ResponseTime.TotalMilliseconds),
        ReceivedAt = response.ReceivedAt,
        ErrorCode = response.ErrorCode,
        ErrorMessage = response.ErrorMessage is { Length: > ErrorMessageMaxLength } message
            ? message[..ErrorMessageMaxLength]
            : response.ErrorMessage,
    };

    public static Search ToDomain(SearchEntity entity) => Search.Restore(
        entity.Id,
        new SearchCriteria(
            Route.Create(Location.Create(entity.Origin), Location.Create(entity.Destination)),
            ShippingDateRange.Create(entity.ShipDateFrom, entity.ShipDateTo)),
        entity.Suppliers.OrderBy(selection => selection.SupplierId, StringComparer.Ordinal).Select(selection => SupplierId.Create(selection.SupplierId)),
        entity.CreatedAt,
        entity.Status,
        entity.CompletedAt,
        entity.Responses.OrderBy(response => response.ReceivedAt).Select(ToDomain));

    public static SupplierResponse ToDomain(SupplierResponseEntity entity) => SupplierResponse.Restore(
        entity.Id,
        SupplierId.Create(entity.SupplierId),
        entity.Outcome,
        entity is { PriceMinorUnits: { } minorUnits, Currency: { } currency } ? Money.FromMinorUnits(minorUnits, currency) : null,
        TimeSpan.FromMilliseconds(entity.ResponseTimeMs),
        entity.ReceivedAt,
        entity.ErrorCode,
        entity.ErrorMessage);
}
