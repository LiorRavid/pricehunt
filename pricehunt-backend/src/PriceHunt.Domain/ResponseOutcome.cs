namespace PriceHunt.Domain;

/// <summary>How a selected supplier's part in a search ended.</summary>
public enum ResponseOutcome
{
    /// <summary>The supplier returned a price.</summary>
    Succeeded,

    /// <summary>The supplier returned an error.</summary>
    Failed,

    /// <summary>The supplier had not responded when the deadline ended the search.</summary>
    TimedOut,

    /// <summary>The call was cancelled before the supplier responded.</summary>
    Cancelled,
}
