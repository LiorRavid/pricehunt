using System.Diagnostics;
using System.Globalization;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PriceHunt.Application.Searches;
using PriceHunt.Domain;

namespace PriceHunt.Infrastructure.Tests.Persistence;

public sealed class DatabaseRegistrationTests
{
    private static readonly SearchCriteria s_criteria = new(
        Route.Create(Location.Create("Haifa"), Location.Create("Rotterdam")),
        ShippingDateRange.Create(new DateOnly(2026, 10, 5), new DateOnly(2026, 10, 12)));

    [Fact]
    [Trait("Requirement", "SV3")]
    public async Task A_write_to_a_locked_database_gives_up_within_the_persistence_timeout()
    {
        await using SqliteTestDatabase database = await SqliteTestDatabase.CreateAsync(("albatross-freight", "Albatross Freight"));
        await using ServiceProvider services = AddInfrastructure(database.FilePath, persistenceTimeout: TimeSpan.FromSeconds(1));
        await using SqliteConnection locker = await HoldWriteLockAsync(database.FilePath);
        var search = Search.Start(s_criteria, [SupplierId.Create("albatross-freight")], new DateTime(2026, 10, 4, 10, 0, 0, DateTimeKind.Utc));
        var watch = Stopwatch.StartNew();

        Func<Task> write = () => services.GetRequiredService<ISearchRepository>().AddAsync(search, TestContext.Current.CancellationToken);

        // SQLITE_BUSY: the database is locked.
        (await write.Should().ThrowAsync<SqliteException>()).Which.SqliteErrorCode.Should().Be(5);
        watch.Elapsed.Should().BeLessThan(TimeSpan.FromSeconds(5), "SQLite would otherwise wait out its 30-second default");
    }

    private static ServiceProvider AddInfrastructure(string databasePath, TimeSpan persistenceTimeout)
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Database:Path"] = databasePath,
                ["Search:PersistenceTimeout"] = persistenceTimeout.ToString("c", CultureInfo.InvariantCulture),
            })
            .Build();

        var services = new ServiceCollection();
        services.AddOptions<SearchOptions>().Bind(configuration.GetSection(SearchOptions.SectionName));
        services.AddInfrastructure(configuration, Path.GetTempPath());
        return services.BuildServiceProvider();
    }

    /// <summary>Opens a second connection that takes the database's write lock, as another process would.</summary>
    private static async Task<SqliteConnection> HoldWriteLockAsync(string databasePath)
    {
        var connection = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = databasePath }.ToString());
        await connection.OpenAsync(TestContext.Current.CancellationToken);
        await using SqliteCommand begin = connection.CreateCommand();
        begin.CommandText = "BEGIN IMMEDIATE;";
        await begin.ExecuteNonQueryAsync(TestContext.Current.CancellationToken);
        return connection;
    }
}
