namespace PriceHunt.Domain;

/// <summary>What a search asks the suppliers for: a route and the days the goods can ship.</summary>
/// <param name="Route">Where the goods ship from and to.</param>
/// <param name="ShippingDates">The days the goods can ship.</param>
public sealed record SearchCriteria(Route Route, ShippingDateRange ShippingDates);
