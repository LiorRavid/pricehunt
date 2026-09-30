using PriceHunt.Application.Suppliers;
using PriceHunt.Domain;

namespace PriceHunt.Application.Searches;

/// <summary>
/// Validates a search request and resolves its suppliers, before anything is streamed: no
/// selection means every supplier, and an unknown id is a validation error.
/// </summary>
/// <param name="catalog">The available suppliers.</param>
public sealed class SearchPlanner(ISupplierCatalog catalog)
{
    /// <summary>Validates <paramref name="request"/> and builds a plan.</summary>
    /// <param name="request">The raw search input.</param>
    /// <returns>A plan, or every validation error at once.</returns>
    public SearchPlanResult Plan(SearchRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        Dictionary<string, string[]> errors = [];

        Route? route = ResolveRoute(request, errors);
        ShippingDateRange? dates = ResolveDates(request, errors);
        IReadOnlyList<IShippingSupplier> suppliers = ResolveSuppliers(request.SupplierIds, errors);

        return route is not null && dates is not null && errors.Count == 0
            ? SearchPlanResult.Valid(new SearchPlan(new SearchCriteria(route, dates), suppliers))
            : SearchPlanResult.Invalid(errors);
    }

    private static Route? ResolveRoute(SearchRequest request, Dictionary<string, string[]> errors)
    {
        if (!Location.TryCreate(request.Origin, out Location? origin, out string? originError))
        {
            errors[SearchRequestFields.Origin] = [originError];
        }

        if (!Location.TryCreate(request.Destination, out Location? destination, out string? destinationError))
        {
            errors[SearchRequestFields.Destination] = [destinationError];
        }

        if (origin is null || destination is null)
        {
            return null;
        }

        if (!Route.TryCreate(origin, destination, out Route? route, out string? routeError))
        {
            errors[SearchRequestFields.Destination] = [routeError];
        }

        return route;
    }

    private static ShippingDateRange? ResolveDates(SearchRequest request, Dictionary<string, string[]> errors)
    {
        if (request.FromDate is null)
        {
            errors[SearchRequestFields.FromDate] = ["A start date is required."];
        }

        if (request.ToDate is null)
        {
            errors[SearchRequestFields.ToDate] = ["An end date is required."];
        }

        if (request is not { FromDate: { } from, ToDate: { } to })
        {
            return null;
        }

        if (!ShippingDateRange.TryCreate(from, to, out ShippingDateRange? dates, out string? datesError))
        {
            errors[SearchRequestFields.ToDate] = [datesError];
        }

        return dates;
    }

    private IReadOnlyList<IShippingSupplier> ResolveSuppliers(IReadOnlyList<string>? requested, Dictionary<string, string[]> errors)
    {
        if (requested is null || requested.Count == 0)
        {
            return catalog.Suppliers;
        }

        HashSet<string> wanted = [.. requested];
        HashSet<string> known = [.. catalog.Suppliers.Select(supplier => supplier.Id.Value)];
        string[] unknown = [.. wanted.Where(id => !known.Contains(id)).Order(StringComparer.Ordinal)];
        if (unknown.Length > 0)
        {
            errors[SearchRequestFields.SupplierIds] = [$"Unknown supplier ids: {string.Join(", ", unknown.Select(id => $"'{id}'"))}."];
            return [];
        }

        return [.. catalog.Suppliers.Where(supplier => wanted.Contains(supplier.Id.Value))];
    }
}
