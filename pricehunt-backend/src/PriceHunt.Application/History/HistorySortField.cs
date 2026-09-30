namespace PriceHunt.Application.History;

/// <summary>The price-history columns a client can sort by.</summary>
public enum HistorySortField
{
    /// <summary>The quote timestamp.</summary>
    Date,

    /// <summary>The origin, then the destination.</summary>
    Route,

    /// <summary>The supplier's display name.</summary>
    Supplier,

    /// <summary>The quoted price; responses without a price sort last.</summary>
    Price,

    /// <summary>How long the supplier took.</summary>
    ResponseTime,
}
