namespace PriceHunt.Domain;

/// <summary>Where a search is in its lifecycle: running, then exactly one terminal state.</summary>
public enum SearchStatus
{
    /// <summary>Suppliers are still being queried.</summary>
    Running,

    /// <summary>Every selected supplier responded before the deadline.</summary>
    Completed,

    /// <summary>The deadline ended the search; silent suppliers were recorded as timed out.</summary>
    TimedOut,

    /// <summary>The client went away or started a new search.</summary>
    Cancelled,

    /// <summary>An unexpected server error ended the search.</summary>
    Faulted,
}
