using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using PriceHunt.Application.Suppliers;
using PriceHunt.Domain;
using PriceHunt.Infrastructure.Persistence;

namespace PriceHunt.Infrastructure.Tests.Persistence;

public sealed class DatabaseInitializerTests
{
    [Fact]
    [Trait("Requirement", "DB3")]
    public async Task Creates_the_file_and_schema_on_first_run()
    {
        await using var database = SqliteTestDatabase.CreateEmpty();
        Directory.Exists(database.Directory).Should().BeFalse();

        await Initializer(database, ("albatross-freight", "Albatross Freight")).InitializeAsync(TestContext.Current.CancellationToken);

        File.Exists(database.FilePath).Should().BeTrue();
        await using PriceHuntDbContext db = database.CreateDbContext();
        (await db.Database.GetAppliedMigrationsAsync(TestContext.Current.CancellationToken)).Should().ContainSingle()
            .Which.Should().EndWith("_InitialCreate");
        (await db.Database.GetPendingMigrationsAsync(TestContext.Current.CancellationToken)).Should().BeEmpty();
    }

    [Fact]
    [Trait("Requirement", "DB3")]
    public async Task The_committed_migrations_match_the_model()
    {
        await using SqliteTestDatabase database = await SqliteTestDatabase.CreateAsync();
        await using PriceHuntDbContext db = database.CreateDbContext();

        db.Database.HasPendingModelChanges().Should().BeFalse("every model change needs a committed migration");
    }

    [Fact]
    public async Task Switches_the_database_to_write_ahead_logging()
    {
        await using var database = SqliteTestDatabase.CreateEmpty();

        await Initializer(database).InitializeAsync(TestContext.Current.CancellationToken);

        await using PriceHuntDbContext db = database.CreateDbContext();
        string mode = await db.Database.SqlQueryRaw<string>("SELECT journal_mode AS Value FROM pragma_journal_mode").SingleAsync(TestContext.Current.CancellationToken);
        mode.Should().Be("wal");
    }

    [Fact]
    [Trait("Requirement", "S1")]
    public async Task Stores_the_supplier_catalogue_and_keeps_names_current()
    {
        await using var database = SqliteTestDatabase.CreateEmpty();
        await Initializer(database, ("albatross-freight", "Albatross Freight")).InitializeAsync(TestContext.Current.CancellationToken);

        await Initializer(database, ("albatross-freight", "Albatross Freight Co."), ("bramblewood-cargo", "Bramblewood Cargo"))
            .InitializeAsync(TestContext.Current.CancellationToken);

        await using PriceHuntDbContext db = database.CreateDbContext();
        (await db.Suppliers.OrderBy(supplier => supplier.Id).Select(supplier => supplier.Name).ToListAsync(TestContext.Current.CancellationToken))
            .Should().Equal("Albatross Freight Co.", "Bramblewood Cargo");
    }

    private static DatabaseInitializer Initializer(SqliteTestDatabase database, params (string Id, string Name)[] suppliers) =>
        new(database, new StubCatalog(suppliers), NullLogger<DatabaseInitializer>.Instance);

    private sealed class StubCatalog((string Id, string Name)[] suppliers) : ISupplierCatalog
    {
        public IReadOnlyList<IShippingSupplier> Suppliers { get; } = [.. suppliers.Select(supplier => new StubSupplier(supplier.Id, supplier.Name))];
    }

    private sealed class StubSupplier(string id, string name) : IShippingSupplier
    {
        public SupplierId Id { get; } = SupplierId.Create(id);

        public string DisplayName { get; } = name;

        public Task<Money> GetQuoteAsync(SearchCriteria criteria, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Not called by the initializer.");
    }
}
