using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;

namespace PriceHunt.Api.Tests;

/// <summary>Runs the API in memory against its own temporary SQLite database, deleted on dispose.</summary>
public sealed class PriceHuntApiFactory : WebApplicationFactory<Program>
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
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.UseSetting("Database:Path", DatabasePath);
    }
}
