using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PriceHunt.Application.Suppliers;

namespace PriceHunt.Infrastructure.Persistence;

/// <summary>
/// Prepares the database at startup: creates the file and schema through the committed migrations,
/// switches to WAL, and upserts the supplier catalogue.
/// </summary>
internal sealed partial class DatabaseInitializer(
    IDbContextFactory<PriceHuntDbContext> contextFactory,
    ISupplierCatalog catalog,
    ILogger<DatabaseInitializer> logger)
{
    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        await using PriceHuntDbContext db = await contextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        string databasePath = new SqliteConnectionStringBuilder(db.Database.GetConnectionString()).DataSource;
        if (Path.GetDirectoryName(databasePath) is { Length: > 0 } directory)
        {
            Directory.CreateDirectory(directory);
        }

        string[] pending = [.. await db.Database.GetPendingMigrationsAsync(cancellationToken).ConfigureAwait(false)];
        await db.Database.MigrateAsync(cancellationToken).ConfigureAwait(false);
        if (pending.Length > 0)
        {
            LogMigrationsApplied(logger, pending.Length, pending, databasePath);
        }
        else
        {
            LogSchemaUpToDate(logger, databasePath);
        }

        await db.Database.ExecuteSqlRawAsync("PRAGMA journal_mode=WAL;", cancellationToken).ConfigureAwait(false);
        await SynchronizeSuppliersAsync(db, cancellationToken).ConfigureAwait(false);
    }

    private async Task SynchronizeSuppliersAsync(PriceHuntDbContext db, CancellationToken cancellationToken)
    {
        Dictionary<string, SupplierEntity> stored = await db.Suppliers.ToDictionaryAsync(supplier => supplier.Id, cancellationToken).ConfigureAwait(false);
        foreach (IShippingSupplier supplier in catalog.Suppliers)
        {
            if (stored.TryGetValue(supplier.Id.Value, out SupplierEntity? row))
            {
                row.Name = supplier.DisplayName;
            }
            else
            {
                db.Suppliers.Add(new SupplierEntity { Id = supplier.Id.Value, Name = supplier.DisplayName });
            }
        }

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    [LoggerMessage(EventId = 2000, Level = LogLevel.Information, Message = "Applied database migrations ({Count}): {Migrations} to {DatabasePath}")]
    private static partial void LogMigrationsApplied(ILogger logger, int count, string[] migrations, string databasePath);

    [LoggerMessage(EventId = 2001, Level = LogLevel.Information, Message = "Database schema is up to date at {DatabasePath}")]
    private static partial void LogSchemaUpToDate(ILogger logger, string databasePath);
}
