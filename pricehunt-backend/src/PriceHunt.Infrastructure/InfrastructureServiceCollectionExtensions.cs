using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using PriceHunt.Application.History;
using PriceHunt.Application.Searches;
using PriceHunt.Application.Suppliers;
using PriceHunt.Infrastructure.Persistence;
using PriceHunt.Infrastructure.Simulation;

namespace PriceHunt.Infrastructure;

/// <summary>Registers the SQLite persistence and the simulated suppliers, and prepares the database.</summary>
public static class InfrastructureServiceCollectionExtensions
{
    /// <summary>Adds the infrastructure implementations of the application's ports.</summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration with the <c>Database</c> and <c>Simulation</c> sections.</param>
    /// <param name="contentRootPath">The folder a relative database path resolves against.</param>
    /// <returns>The same service collection.</returns>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration, string contentRootPath)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddOptions<DatabaseOptions>()
            .Bind(configuration.GetSection(DatabaseOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();
        services.AddOptions<SimulationOptions>()
            .Bind(configuration.GetSection(SimulationOptions.SectionName))
            .ValidateOnStart();
        services.AddSingleton<IValidateOptions<SimulationOptions>, SimulationOptionsValidator>();

        services.AddDbContextFactory<PriceHuntDbContext>((serviceProvider, options) =>
        {
            string databasePath = Path.GetFullPath(serviceProvider.GetRequiredService<IOptions<DatabaseOptions>>().Value.Path, contentRootPath);

            // While another connection holds the database's lock, SQLite waits for this long and
            // ignores cancellation tokens, so this is what bounds each write (Search:PersistenceTimeout).
            TimeSpan writeTimeout = serviceProvider.GetRequiredService<IOptions<SearchOptions>>().Value.PersistenceTimeout;
            options.UseSqlite(new SqliteConnectionStringBuilder
            {
                DataSource = databasePath,
                DefaultTimeout = (int)Math.Ceiling(writeTimeout.TotalSeconds),
            }.ToString());
        });

        services.AddSingleton<ISearchRepository, SqliteSearchRepository>();
        services.AddSingleton<IPriceHistoryQuery, SqlitePriceHistoryQuery>();
        services.AddSingleton<ISupplierCatalog, SimulatedSupplierCatalog>();
        services.AddSingleton<DatabaseInitializer>();
        return services;
    }

    /// <summary>
    /// Creates or migrates the database, syncs the supplier catalogue, and closes searches a crash
    /// left running. Call once at startup, before the server accepts requests.
    /// </summary>
    /// <param name="services">The application's root service provider.</param>
    /// <param name="cancellationToken">A token to cancel startup.</param>
    /// <returns>A task that completes when the database is ready.</returns>
    public static async Task InitializeDatabaseAsync(this IServiceProvider services, CancellationToken cancellationToken = default)
    {
        await using AsyncServiceScope scope = services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<DatabaseInitializer>().InitializeAsync(cancellationToken).ConfigureAwait(false);
        await scope.ServiceProvider.GetRequiredService<InterruptedSearchRecovery>().RecoverAsync(cancellationToken).ConfigureAwait(false);
    }
}
