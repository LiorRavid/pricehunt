using PriceHunt.Application.Suppliers;
using PriceHunt.Domain;

namespace PriceHunt.Application.Searches;

/// <summary>A validated search, ready to run.</summary>
/// <param name="Criteria">What to ask the suppliers for.</param>
/// <param name="Suppliers">The suppliers to query, in catalogue order.</param>
public sealed record SearchPlan(SearchCriteria Criteria, IReadOnlyList<IShippingSupplier> Suppliers);
