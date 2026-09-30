using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using PriceHunt.Infrastructure.Persistence;

namespace PriceHunt.Infrastructure.Tests.Persistence;

/// <summary>A real SQLite database in a temporary file, deleted (with its WAL files) on dispose.</summary>
internal sealed class SqliteTestDatabase : IDbContextFactory<PriceHuntDbContext>, IAsyncDisposable
{
    private readonly DbContextOptions<PriceHuntDbContext> _options;

    private SqliteTestDatabase(string directory)
    {
        Directory = directory;
        FilePath = Path.Combine(directory, "pricehunt.db");
        _options = new DbContextOptionsBuilder<PriceHuntDbContext>()
            .UseSqlite(new SqliteConnectionStringBuilder { DataSource = FilePath }.ToString())
            .Options;
    }

    public string Directory { get; }

    public string FilePath { get; }

    /// <summary>Creates an empty database location; nothing exists on disk until something connects.</summary>
    public static SqliteTestDatabase CreateEmpty() =>
        new(Path.Combine(Path.GetTempPath(), "pricehunt-tests", Guid.NewGuid().ToString("N")));

    /// <summary>Creates a migrated database with the given suppliers.</summary>
    public static async Task<SqliteTestDatabase> CreateAsync(params (string Id, string Name)[] suppliers)
    {
        SqliteTestDatabase database = CreateEmpty();
        System.IO.Directory.CreateDirectory(database.Directory);
        await using PriceHuntDbContext db = database.CreateDbContext();
        await db.Database.MigrateAsync();
        db.Suppliers.AddRange(suppliers.Select(supplier => new SupplierEntity { Id = supplier.Id, Name = supplier.Name }));
        await db.SaveChangesAsync();
        return database;
    }

    public PriceHuntDbContext CreateDbContext() => new(_options);

    public ValueTask DisposeAsync()
    {
        SqliteConnection.ClearAllPools();
        if (System.IO.Directory.Exists(Directory))
        {
            System.IO.Directory.Delete(Directory, recursive: true);
        }

        return ValueTask.CompletedTask;
    }
}
