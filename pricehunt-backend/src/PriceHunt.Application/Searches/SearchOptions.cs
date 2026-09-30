using System.ComponentModel.DataAnnotations;

namespace PriceHunt.Application.Searches;

/// <summary>Search limits, bound from the <c>Search</c> configuration section and validated at startup.</summary>
public sealed class SearchOptions
{
    /// <summary>The configuration section name.</summary>
    public const string SectionName = "Search";

    /// <summary>Gets or sets how long a search may run before it ends as timed out.</summary>
    [Range(typeof(TimeSpan), "00:00:01", "00:01:00")]
    public TimeSpan MaxDuration { get; set; } = TimeSpan.FromSeconds(6);

    /// <summary>Gets or sets how long one database write may take; writes never use the request token.</summary>
    [Range(typeof(TimeSpan), "00:00:01", "00:01:00")]
    public TimeSpan PersistenceTimeout { get; set; } = TimeSpan.FromSeconds(5);
}
