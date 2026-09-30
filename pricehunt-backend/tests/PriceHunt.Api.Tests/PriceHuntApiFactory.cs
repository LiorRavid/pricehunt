using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;

namespace PriceHunt.Api.Tests;

/// <summary>
/// Runs the API in memory with its real configuration, against its own temporary SQLite database
/// (deleted on dispose). Subclasses swap in test doubles.
/// </summary>
public class PriceHuntApiFactory : WebApplicationFactory<Program>
{
    private readonly string _databaseDirectory = Path.Combine(Path.GetTempPath(), "pricehunt-api-tests", Guid.NewGuid().ToString("N"));

    public string DatabasePath => Path.Combine(_databaseDirectory, "pricehunt.db");

    public override async ValueTask DisposeAsync()
    {
        await base.DisposeAsync();
        SqliteConnection.ClearAllPools();
        if (Directory.Exists(_databaseDirectory))
        {
            Directory.Delete(_databaseDirectory, recursive: true);
        }

        GC.SuppressFinalize(this);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.UseSetting("Database:Path", DatabasePath);
        builder.ConfigureTestServices(ConfigureTestServices);
    }

    /// <summary>Replaces services after the application registered its own.</summary>
    protected virtual void ConfigureTestServices(IServiceCollection services)
    {
    }
}
