using System.ComponentModel.DataAnnotations;

namespace PriceHunt.Infrastructure.Persistence;

/// <summary>Where the SQLite database lives, bound from the <c>Database</c> section.</summary>
internal sealed class DatabaseOptions
{
    public const string SectionName = "Database";

    /// <summary>Gets or sets the database file path; a relative path resolves against the API's content root.</summary>
    [Required]
    public string Path { get; set; } = "App_Data/pricehunt.db";
}
